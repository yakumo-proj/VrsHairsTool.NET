using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Mono.Options;
using VrsHairPresetsManager;

namespace Sample {
    class MainClass {
		static string GetPresetDirectory()
		{
			if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) {
				string home = Environment.GetEnvironmentVariable("HOME");
				return Path.Combine(home,
				  "Library/Application Support/com.Company.ProductName/hair_presets");
			} else {
				string userProfile = Environment.GetEnvironmentVariable("USERPROFILE");
				return Path.Combine(userProfile,
					@"AppData\LocalLow\pixiv\VRoidStudio\hair_presets");
			}
		}

		static int NextDirectoryNumber()
        {
            var dirInfo = new DirectoryInfo(GetPresetDirectory());
            if (!dirInfo.Exists) {
                dirInfo.Create();
                return 0;
            }
            int o = 0;
            return dirInfo.GetDirectories("preset*")
                .Where(x => x.GetFiles("preset.json").Length > 0)
                .Select(x => int.TryParse(x.Name.Substring(6), out o) ? o : -1)
                .Max() + 1;
        }

        public static void Main(string[] args)
        {
            string presetNoStr = "";
            string dispName = "";

            var optionSet = new OptionSet()
              .Add("d|dispname=", "Merged Hair-Preset Display Name", v => dispName = v)
              .Add("p|preset-dir-num=", "Preset Directory Number", v => presetNoStr = v);

            var filepaths = optionSet.Parse(args);
            if (filepaths.Count < 2) {
                Console.WriteLine("too few arguments.");
                return;
            }

            int presetNo;
            try
            {
                presetNo = int.Parse(presetNoStr);
			} catch (Exception) {
                presetNo = NextDirectoryNumber();
            }
			if (dispName.Length == 0) {
                dispName = "プリセット" + presetNo;
            }

            var destPath = Path.Combine(GetPresetDirectory(), String.Format("preset{0}", presetNo));
            var merged = new VRoidMergedHairsNode(filepaths[0], destPath, dispName);
			try {
                merged.MergeInitialize();
				foreach (var other in filepaths.Skip(1).Select(x => new VRoidHairsNode(x))) {
                    merged.Merge(other);
                }
                merged.MergeFinalize();
                Console.WriteLine(destPath + " created.");
			} catch (Exception e) {
				if (Directory.Exists(destPath)) {
                    Directory.Delete(destPath, true);
                }
                Console.WriteLine(e.ToString());
            }
        }
    }
}
