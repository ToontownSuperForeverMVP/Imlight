using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Imlight.CoreLib.Mods.ShadowSchool;
using Xunit;

namespace Imlight.CoreLib.Tests.Mods.ShadowSchool;

/// <summary>
/// The Shadow equipment catalogue (ShadowGear.cs): eight slots at ten tiers, levels 5 to 50, every piece its own
/// template name and id, and the vendor's spiraldb inventory listing exactly those ids. These are the facts that
/// have to hold for the server to hand the client a shop that matches the templates it builds; the items' stat
/// values come from the donors and are checked against the running server, not here.
/// </summary>
public sealed class ShadowGearCatalogTests {

    private static readonly int[] s_tiers = [5, 10, 15, 20, 25, 30, 35, 40, 45, 50];
    private static readonly string[] s_slots = ["Hat", "Robe", "Boots", "Wand", "Athame", "Amulet", "Ring", "Deck"];

    [Fact]
    public void ThereIsAPieceForEverySlotAndTier() {
        Assert.Equal(s_tiers, ShadowGear.Tiers);
        Assert.Equal(s_slots, ShadowGear.Slots);

        var pieces = ShadowGear.Pieces.ToList();
        Assert.Equal(s_slots.Length * s_tiers.Length, pieces.Count);
        Assert.Equal(pieces.Count, pieces.Select(p => (p.Slot, p.Level)).Distinct().Count());
        Assert.Equal(pieces.Count, pieces.Select(p => p.TemplateId).Distinct().Count());
        Assert.Equal(pieces.Count, pieces.Select(p => p.ObjectName).Distinct().Count());
        Assert.Equal(pieces.Count, pieces.Select(p => p.FilePath).Distinct().Count());
    }

    [Fact]
    public void EveryPieceAsksForItsOwnSlotAndTier() {
        foreach (var piece in ShadowGear.Pieces) {
            Assert.Contains(piece.Slot, s_slots);
            Assert.Contains(piece.Level, s_tiers);
            Assert.Equal($"ShadowSchool-{piece.Slot}-{piece.Level}", piece.ObjectName);
            Assert.Equal($"ObjectData/ShadowSchool/{piece.Slot}-{piece.Level}.xml", piece.FilePath);
            Assert.Equal($"ShadowSchool_Item_{piece.Slot}_{piece.Level}", piece.DisplayNameKey);
            Assert.StartsWith("ObjectData/", piece.Donor);
            Assert.EndsWith(".xml", piece.Donor);
        }
    }

    [Fact]
    public void ATemplateIdIsItsNameHashedTheWayTheGameHashesNames() {
        var piece = new ShadowGearPiece("Hat", 5, "ObjectData/Tier1/Hats/Hat-T1-013.xml");

        Assert.Equal(Imcodec.Cryptography.StringHash.Compute("ShadowSchool-Hat-5"), piece.TemplateId);
        // The client keys templates by this id, so it has to be the one the inventory file lists.
        Assert.Equal(1622599881u, piece.TemplateId);
    }

    [Fact]
    public void EveryNameIsTieredAndUnique() {
        var names = ShadowGear.Pieces.Select(p => p.DisplayName).ToList();

        Assert.Equal(names.Count, names.Distinct().Count());
        Assert.Equal("Novice's Shadow Hat", ShadowGear.Pieces.First(p => p.Slot == "Hat" && p.Level == 5).DisplayName);
        Assert.Equal("Umbral Shadow Deck", ShadowGear.Pieces.First(p => p.Slot == "Deck" && p.Level == 50).DisplayName);
    }

    [Fact]
    public void EveryTierHasATitle() {
        foreach (var level in s_tiers) {
            Assert.False(string.IsNullOrWhiteSpace(ShadowGear.TierTitle(level)));
        }

        Assert.Throws<ArgumentOutOfRangeException>(() => ShadowGear.TierTitle(1));
    }

    [Fact]
    public void TheVendorSellsExactlyTheCatalogue() {
        var path = VendorInventoryPath();
        if (path is null) {
            // The test bin is not inside the repository: nothing to compare the catalogue with.
            return;
        }

        using var document = JsonDocument.Parse(File.ReadAllText(path));
        var root = document.RootElement;

        Assert.Equal(1880000101u, root.GetProperty("TemplateID").GetUInt32());
        Assert.Equal("ShadowSchool_VendorShopTitle", root.GetProperty("ShopTitle").GetString());

        var stocked = root.GetProperty("Inventory").EnumerateArray().Select(e => e.GetUInt32()).ToList();
        var catalogue = ShadowGear.Pieces.Select(p => p.TemplateId).ToList();

        Assert.Equal(catalogue, stocked);
    }

    /// <summary>The mod's vendor inventory, found by walking up from the test bin into the repository.</summary>
    private static string? VendorInventoryPath() {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent) {
            var candidate = Path.Combine(directory.FullName, "src", "Imlight.Director", "mods", "ShadowSchool",
                "spiraldb", "NpcInventory", "NPCInventories_ShadowSchool-Vendor.json");
            if (File.Exists(candidate)) {
                return candidate;
            }
        }

        return null;
    }

}
