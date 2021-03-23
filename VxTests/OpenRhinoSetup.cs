using NUnit.Framework;
using System;
using Rhino;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using System.Xml.Linq;
using System.Linq;
using System.Runtime.Remoting.Contexts;
using System.Diagnostics;
using WinFormsApp;
using System.Windows.Forms;

namespace MxTests
{
  [SetUpFixture]
  public class OpenRhinoSetup
  {
    internal static string RhinoSystemDir { get; private set; }

    private static Exception to_throw;
    internal static string SettingsFile { get; private set; }

    internal const string SettingsFileName = "VxTests.testsettings.xml";
    internal static string SettingsDir { get; private set; }

    internal static string GetCallerFilePath([System.Runtime.CompilerServices.CallerFilePath] string filePath = "")
    {
      return filePath;
    }

    static OpenRhinoSetup()
    {
      SettingsDir = Path.GetDirectoryName(GetCallerFilePath());

        //TestContext.CurrentContext.TestDirectory; //this will be the location where the .dll is found
      SettingsFile = Path.Combine(SettingsDir, SettingsFileName);

      if (File.Exists(SettingsFile))
      {
        SettingsXml = XDocument.Load(SettingsFile);
        RhinoSystemDir = SettingsXml.Descendants("RhinoSystemDirectory").FirstOrDefault()?.Value ?? null;
      }
      else
        SettingsXml = new XDocument();
    }

    internal static XDocument SettingsXml { get; set; }

    internal static void ScanFolders(string heading, List<string> testModels)
    {
      var test_folders = new List<string>();
      foreach (var mdir in OpenRhinoSetup.SettingsXml.Descendants(heading).Descendants("ModelDirectory"))
      {
        bool optional = mdir.Attribute("optional")?.Value != "false"; //defaults to true

        string attempt = mdir.Value;
        if (!Path.IsPathRooted(mdir.Value))
        {
          attempt = Path.Combine(OpenRhinoSetup.SettingsDir, attempt);
          attempt = Path.GetFullPath(attempt);
        }

        if (Directory.Exists(attempt))
        {
          test_folders.Add(attempt);
        }
        else if (!optional)
        {
          to_throw = new InvalidOperationException($"Could not find required directory: \"{mdir.Value}\".");
          break;
        }
      }

      foreach (string folder in test_folders)
      {
        if (Directory.Exists(folder))
          testModels.AddRange(
              Directory.GetFiles(folder, @"*.3dm", SearchOption.AllDirectories)
              );
      }

      testModels.RemoveAll(f => Path.GetFileName(f).StartsWith("#", System.StringComparison.InvariantCultureIgnoreCase));
      testModels.RemoveAll(f => Path.GetFileName(f).EndsWith("bak", System.StringComparison.InvariantCultureIgnoreCase));
    }

    private Rhino.Runtime.InProcess.RhinoCore rhinoCore; //do NOT reference this by its RhinoCommon name
    private MainForm mainform;

    [OneTimeSetUp, STAThread]
    public void OneTimeSetUp()
    {
      if (to_throw != null) throw to_throw;

      RhinoInside.Resolver.Initialize();
      if (RhinoSystemDir != null) RhinoInside.Resolver.RhinoSystemDirectory = RhinoSystemDir;
      else RhinoSystemDir = RhinoInside.Resolver.RhinoSystemDirectory;
      TestContext.WriteLine("RhinoSystemDir is: " + RhinoSystemDir + ".");


      Application.Run(new MainForm());
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
      (rhinoCore as IDisposable)?.Dispose();
      rhinoCore = null;
    }
  }

  [TestFixture]
  public class _OpenRhinoTests
  {
    [Test]
    public void SettingsFileExists()
    {
      Trace.WriteLine($"Rhino folder is {OpenRhinoSetup.RhinoSystemDir}.");

      Assert.IsTrue(File.Exists(OpenRhinoSetup.SettingsFile),
          $"File setting does not exist. Expected '{OpenRhinoSetup.SettingsFile}' in '{OpenRhinoSetup.SettingsDir}'.");
    }
  }
}
