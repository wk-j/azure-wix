using SIO = System.IO;
using System.Linq;
using WixSharp;
using System;
using System.Collections.Generic;

class Config {
    public string ProjectDir { set; get; }
    public string TargetDir { get; } = $"%ProgramFiles%\\wk-j\\MyApp";
    public string InstallerName { get; set; }
    public string ToolLocation { get; } = @"C:\Program Files (x86)\WiX Toolset v3.11\bin";
    public string Version { set; get; }
}

class FileFilter {
    private readonly System.IO.DirectoryInfo TopDir;

    public string Root => TopDir.FullName;

    public FileFilter(string dir) {
        TopDir = new System.IO.DirectoryInfo(dir);
    }

    System.IO.FileInfo[] GetFiles(string pattern) {
        return TopDir.GetFiles(pattern);
    }

    System.IO.FileInfo[] GetFiles(string pattern, string subDir) {
        return new System.IO.DirectoryInfo(System.IO.Path.Combine(TopDir.FullName, subDir)).GetFiles(pattern);
    }


    File ToWixFile(System.IO.FileInfo file) {
        return new File(file.FullName) {
            Permissions = new FilePermission[] {
                    new FilePermission("Everyone", GenericPermission.All)
                }
        };
    }

    public File GetFile(string path) {
        var full = System.IO.Path.Combine(TopDir.FullName, path);
        return new File(full);
    }

    public File[] GetWixFiles(string pattern) {
        return GetFiles(pattern).Select(ToWixFile).ToArray();
    }

    public File[] GetWixFiles(string pattern, string subDir) {
        return GetFiles(pattern, subDir).Select(ToWixFile).ToArray();
    }
}

class Utiltiy {
    public static void CreateShortcut(File file) {
        file.Shortcuts = new FileShortcut[] {
                new FileShortcut(file.Id, "INSTALLDIR"),
                new FileShortcut(file.Id, "%Desktop%")
            };
    }
}

class Program {
    static Dir GetTopDir(FileFilter filter) {
        var exe = filter.GetWixFiles("*.exe");
        var json = filter.GetWixFiles("*.json");
        var dll = filter.GetWixFiles("*.dll");
        var config = filter.GetWixFiles("*.config");
        var md5 = filter.GetWixFiles("*.md5");

        json.Where(x => x.Name == "appsettings.json").ForEach(x => x.SetComponentPermanent(true));
        md5.ForEach(x => x.SetComponentPermanent(true));

        var files = new List<File>();
        files.AddRange(exe);
        files.AddRange(config);
        files.AddRange(dll);
        files.AddRange(json);
        files.AddRange(md5);

        var dir = new Dir(".", files.ToArray());
        return dir;
    }

    static WixEntity[] GetEasyCaptureStructures(string root) {
        var filter = new FileFilter(root);

        var dirs = new WixEntity[] {
                GetTopDir(filter),
                //new Dir("x86", filter.GetWixFiles("*.dll", "x86")),
                //new Dir("x64", filter.GetWixFiles("*.dll", "x64")),
                //new Dir("logs", new File [] { }),
                //new Dir("wwwroot", new Files(System.IO.Path.Combine(root, @"wwwroot\*.*")))
            };

        return dirs.ToArray();
    }

    static Config CreateConfig() {
        var projectDir = new SIO.DirectoryInfo(".").Parent.Parent.FullName;
        projectDir = SIO.Path.Combine(projectDir, "publish/W");

        Console.WriteLine(projectDir);

        var parser = DepsParser.Parser.Search("../../src/MyApp", "MyApp.deps.json");
        var version = parser.GetLibraryVersion("MyApp").Trim();

        var config = new Config {
            InstallerName = $"MyApp.{version}",
            ProjectDir = projectDir,
            Version = version
        };
        return config;
    }

    static void Main(string[] args) {
        var config = CreateConfig();

        if (args.Length == 1) {
            // config.Version = config.Version + "." + args[0];
            Console.WriteLine("== Args ==");
            foreach (var item in args) {
                Console.WriteLine($" >> {0}", item);
            }
        }

        Console.WriteLine($" >> Version {0}", config.Version);

        var wixDir = Environment.GetEnvironmentVariable("WIXSHARP_WIXDIR");
        if (string.IsNullOrEmpty(wixDir)) {
            Environment.SetEnvironmentVariable("WIXSHARP_WIXDIR", config.ToolLocation, EnvironmentVariableTarget.Process);
        }

        var structure = GetEasyCaptureStructures(config.ProjectDir);
        var topDir = new Dir(config.TargetDir, structure);
        var project = new Project(config.InstallerName, topDir);
        project.UI = WUI.WixUI_InstallDir;
        project.UpgradeCode = Guid.Parse("a6ac098c-117f-5eb5-b485-48fbd83dee4e");
        project.ProductId = Guid.NewGuid();
        project.Version = new Version(config.Version);
        project.MajorUpgrade = new MajorUpgrade { AllowSameVersionUpgrades = true, DowngradeErrorMessage = "Higher version already installed" };

        var exe = project.AllFiles.Where(x => x.Id == "MyApp.exe").FirstOrDefault();
        Compiler.BuildMsi(project);
    }
}