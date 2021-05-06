using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Json;

namespace VrsHairPresetsManager {
    class VRoidHairsNode {
        static Encoding Enc = new UTF8Encoding(false);
		protected string TexturePath => Path.Combine("materials", "rendered_textures");
		public string SourcePath { get; set; }
        public JsonObject JsonParsed { get; set; }
		public JsonArray Hairs {
            get { return JsonParsed["Hairishes"] as JsonArray; }
            set { JsonParsed["Hairishes"] = value; }
        }
		public JsonArray Materials {
            get { return JsonParsed["_MaterialSet"]["_Materials"] as JsonArray; }
            set { JsonParsed["_MaterialSet"]["_Materials"] = value; }
        }
		public JsonArray Bones {
            get { return JsonParsed["_HairBoneStore"]["Groups"] as JsonArray; }
            set { JsonParsed["_HairBoneStore"]["Groups"] = value; }
        }
		public string DispName {
            get { return JsonParsed["_DisplayName"]; }
            set { JsonParsed["_DisplayName"] = value; }
        }
		public JsonObject Meta {
            get { return JsonParsed["_MetaData"] as JsonObject; }
            set { JsonParsed["_MetaData"] = value; }
        }
        public byte[] GetTextures(string textureId)
        {
            var path = Path.Combine(SourcePath, TexturePath, textureId + ".png");
            using (FileStream fileStream = File.OpenRead(path)) {
                byte[] buf = new byte[fileStream.Length];
                int offset = 0;
                while (offset < fileStream.Length) {
                    offset += fileStream.Read(buf, offset, (int)fileStream.Length - offset);
                }
                return buf;
            }
        }
        public void Save()
        {
            using (var sw = new StreamWriter(Path.Combine(SourcePath, "preset.json"), false, Enc)) { 
                sw.Write(JsonParsed.ToString());
                sw.Close();
            }
        }
        public Dictionary<string, string> TextureIds { get; set; }
        public VRoidHairsNode(string sourcePath, bool isCreate = false)
		{
            if (isCreate == false) {
                using (var sr = new StreamReader(Path.Combine(sourcePath, "preset.json"), Enc)) {
                    JsonParsed = JsonValue.Load(sr) as JsonObject;
                    sr.Close();
                }
            } else {
                JsonParsed = new JsonObject();
			}
            SourcePath = sourcePath;
        }
    }
}
