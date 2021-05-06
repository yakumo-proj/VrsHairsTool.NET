using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Json;

namespace VrsHairPresetsManager
{
	using UuidDictionary = Dictionary<string, string>;
	class VRoidMergedHairsNode : VRoidHairsNode
	{
		public string DestPath { get; set; }

		public VRoidMergedHairsNode(string sourcePath, string destPath, string dispName)
			: base(sourcePath)
		{
			this.DispName = dispName;
			this.DestPath = destPath;
		}

		public void Merge(VRoidHairsNode other)
		{
			string NewID() => Guid.NewGuid().ToString();

			//現在のマテリアルID
			var ids = Materials.ToDictionary(x => (string)x["_Id"], x => x);

			//IDが競合しないもの
			var notConflicts = other.Materials.Where(x => !ids.ContainsKey(x["_Id"]));
			var materialTranslated = new UuidDictionary();

			// テクスチャを全コピー
			var sourceDir = new DirectoryInfo(Path.Combine(other.SourcePath, TexturePath));
			foreach (FileInfo fileInfo in sourceDir.GetFiles()) {
				var destPath = Path.Combine(this.DestPath, TexturePath, fileInfo.Name);
				if (!File.Exists(destPath)) fileInfo.CopyTo(destPath, true);
			}

			// マテリアルIDが競合する場合→パラメータが別なら新規IDを振ってマージ
			string[] Colors = { "_Color", "_ShadeColor", "_HighlightColor", "_OutlineColor" };
			string[] RGBs = { "r", "g", "b" };

			var textureTrans = new UuidDictionary();
			var conflicts = other.Materials.Where(x =>
				ids.ContainsKey(x["_Id"]) &&
				Colors.Any(color => RGBs.Any(rgb => ids[x["_Id"]][color][rgb] != x[color][rgb]))
			).Select(x => {
				x["_Id"] = materialTranslated[x["_Id"]] = NewID();
				var newTextureID = NewID();
				File.Copy(
					Path.Combine(other.SourcePath, TexturePath, x["_MainTextureId"] + ".png"),
					Path.Combine(DestPath, TexturePath, newTextureID + ".png"),
					true
				);
				x["_MainTextureId"] = newTextureID;
				return x;
			});


			Materials = new JsonArray(Materials.Concat(notConflicts).Concat(conflicts));

			var hairTranslated = new UuidDictionary();

			//ローカル関数
			JsonObject updateMaterialKey(JsonObject node)
			{
				string valout;
				if (materialTranslated.TryGetValue((string)node["Param"]["_MaterialValueGUID"], out valout)) {
					node["Param"]["_MaterialValueGUID"] = valout;
				}
				if (materialTranslated.TryGetValue((string)node["Param"]["_MaterialInheritedValueGUID"], out valout)) {
					node["Param"]["_MaterialInheritedValueGUID"] = valout;
				}
				return node;
			}

			// 髪のマージ
			var hairsConverted = other.Hairs.Where(x => x["Type"] == 2).OfType<JsonObject>().Select(hairGroup => {
				hairGroup["Id"] = hairTranslated[hairGroup["Id"]] = NewID();
				hairGroup["Children"] = new JsonArray(
					hairGroup["Children"].OfType<JsonObject>().Select(hair => {
						hair["Id"] = hairTranslated[hair["Id"]] = NewID();
						return updateMaterialKey(hair);
					})
				);
				return updateMaterialKey(hairGroup);
			});
			Hairs = new JsonArray(Hairs.ToList().Concat(hairsConverted));


			// 髪のボーン
			var bonesChanged = other.Bones
				.Select(bone => {
					var hairs = bone["Hairs"].OfType<JsonObject>()
						.Where(hairId => hairTranslated.ContainsKey(hairId))
						.Select(hairId => hairTranslated[hairId]);
					if (hairs.Count() == 0) {
						return null;
					}
					bone["Id"] = NewID();
					bone["Hairs"] = new JsonArray(hairs.Select(x => (JsonValue)x));
					bone["Joints"] = new JsonArray(
						bone["Joints"].OfType<JsonObject>().Select(joint => {
							joint["Name"] = "HairJoint-" + NewID();
							return joint;
						})
					);
					bone["AxisHintHairIds"] = new JsonArray(
						bone["AxisHintHairIds"].OfType<JsonObject>()
							.Where(hairId => hairTranslated.ContainsKey(hairId))
							.Select(hairId => (JsonValue)hairTranslated[hairId])
					);
					return bone;
				})
				.Where(x => x != null);

			Bones = new JsonArray(Bones.Concat(bonesChanged));
		}

		public void MergeFinalize()
		{
			System.Text.Encoding enc = new System.Text.UTF8Encoding(false);
			using (var sw = new StreamWriter(Path.Combine(this.DestPath, "preset.json"), false, enc)) {
				sw.Write(JsonParsed.ToString());
				sw.Close();
			}
		}

		public void MergeInitialize()
		{
			Directory.CreateDirectory(Path.Combine(DestPath, TexturePath));
			DirectoryInfo sourceDir = new DirectoryInfo(Path.Combine(this.SourcePath, TexturePath));
			foreach (FileInfo fileInfo in sourceDir.GetFiles()) {
				fileInfo.CopyTo(Path.Combine(this.DestPath, TexturePath, fileInfo.Name), true);
			}
		}
	}
}
