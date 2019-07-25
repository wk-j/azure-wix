
var version = Argument("version", "0.1.0");

var settings = new DotNetCoreMSBuildSettings();
settings.Properties["Version"] = new string[] { version };

Task("Publish").Does(() => {
    CleanDirectory(".publish/W");
    DotNetCorePublish("src/MyApp", new DotNetCorePublishSettings {
        OutputDirectory = ".publish/W",
        MSBuildSettings = settings
    });
});

Task("Installer")
    .IsDependentOn("Publish")
        .Does(() => {
        DotNetCorePublish("src/MyInstaller");
    });

var target = Argument("target", "publish");
RunTarget(target);