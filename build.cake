
var version = Argument("vv", "0.1.0");

var settings = new DotNetCoreMSBuildSettings();
settings.Properties["Version"] = new string[] { version };

Task("Publish").Does(() => {
    var dir= ".publish/W";
    CreateDirectory(dir);
    CleanDirectory(dir);
    DotNetCorePublish("src/MyApp", new DotNetCorePublishSettings {
        OutputDirectory = dir, 
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