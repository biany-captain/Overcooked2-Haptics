using System;
using System.Collections.Generic;
using System.IO;

namespace OC2Haptic.Core
{
    public sealed class RewardSoundPack
    {
        public const string DefaultId="turbo_swing";
        public const string CustomStarId="custom-v17";
        public readonly string Id,Name;
        private readonly string[] files;
        private RewardSoundPack(string id,string name,string[] files){Id=id;Name=name;this.files=files;}
        public string FileFor(int star){return files[star-1];}
        public static RewardSoundPack Custom(string third,string fourth)
        {return new RewardSoundPack(CustomStarId,"自定义四星音乐",new[]{null,null,third,fourth});}
        public static bool IsRetired(string id)
        {return string.Equals(id,"joyful-cartoon-jazz",StringComparison.OrdinalIgnoreCase) || string.Equals(id,"crisp-reward",StringComparison.OrdinalIgnoreCase);}
        public static RewardSoundPack[] Discover(string root,Action<string> log)
        {
            var packs=new List<RewardSoundPack>();
            if(!Directory.Exists(root))return packs.ToArray();
            string[] directories;
            try{directories=Directory.GetDirectories(root);Array.Sort(directories,StringComparer.OrdinalIgnoreCase);}
            catch(Exception error){if(log!=null)log("Sound pack discovery unavailable: "+error.Message);return packs.ToArray();}
            foreach(string folder in directories)
            {
                string id=Path.GetFileName(folder);if(IsRetired(id) || string.Equals(id,CustomStarId,StringComparison.OrdinalIgnoreCase))continue;
                string path=Path.Combine(folder,"pack.ini");if(!File.Exists(path))continue;
                try
                {
                    var values=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
                    foreach(string raw in File.ReadAllLines(path))
                    {
                        string line=raw.Trim();if(line.Length==0 || line.StartsWith("#") || line.StartsWith(";"))continue;
                        int split=line.IndexOf('=');if(split<1)throw new FormatException("Expected key=value.");
                        values.Add(line.Substring(0,split).Trim(),line.Substring(split+1).Trim());
                    }
                    string name=values["Name"].Trim();if(name.Length==0)throw new FormatException("Missing display name.");
                    var files=new string[4];
                    for(int i=0;i<4;i++)
                    {
                        string file=values["Star"+(i+1)];
                        if(file.Length==0 || Path.GetFileName(file)!=file || !string.Equals(Path.GetExtension(file),".wav",StringComparison.OrdinalIgnoreCase))
                            throw new FormatException("Star files must be same-folder WAV filenames.");
                        files[i]=Path.Combine(folder,file);
                    }
                    packs.Add(new RewardSoundPack(id,name,files));
                }
                catch(Exception error){if(log!=null)log("Sound pack skipped | path="+path+" reason="+error.Message);}
            }
            return packs.ToArray();
        }
    }
}
