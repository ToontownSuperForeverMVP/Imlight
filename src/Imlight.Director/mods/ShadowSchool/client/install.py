"""
Shadow School client installer.

    python install.py install   [--game DIR] [--generated DIR]
    python install.py uninstall [--game DIR]
    python install.py status    [--game DIR]

install
  1. Checks the client: WizardGraphicalClient.exe must be the supported build (exe_patch.py), and every game file the
     mod replaces must still be the one the server built the mod from (generated/manifest.json).
  2. Keeps the originals once: Bin/WizardGraphicalClient.exe.shadowschool-backup and
     Data/GameData/Root.wad.shadowschool-backup (a backup that already exists is checked, never overwritten).
  3. Writes Root.wad = the original + the mod's files (generated/root, built by the server at start), and the
     patched WizardGraphicalClient.exe. Both are written to a temporary file first, then swapped in.
  4. Records what it wrote in Data/GameData/ShadowSchool.install.json.
uninstall puts the backups back. status tells what is installed.

Start the server once with the mod enabled before installing: it writes generated/. Install again after the server's
mod content changes (the server logs "Mods put N files in Root.wad"). The official Wizard101 launcher replaces both
files when it patches the game; installing again is then needed.

Only the Python standard library is used.
"""

import argparse
import glob
import hashlib
import json
import os
import struct
import sys
import zlib

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import exe_patch  # noqa: E402

DEFAULT_GAME = r"C:\ProgramData\KingsIsle Entertainment\Wizard101"
BACKUP = ".shadowschool-backup"
STATE = "ShadowSchool.install.json"
MOD_DIR = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))


def default_generated():
    """Find the mod output written beside the running Director build."""
    source_output = os.path.join(MOD_DIR, "generated")
    if os.path.isfile(os.path.join(source_output, "manifest.json")):
        return source_output

    director_dir = os.path.dirname(os.path.dirname(MOD_DIR))
    candidates = glob.glob(os.path.join(director_dir, "bin", "*", "*", "mods", "ShadowSchool", "generated"))
    candidates = [path for path in candidates if os.path.isfile(os.path.join(path, "manifest.json"))]
    if candidates:
        return max(candidates, key=lambda path: os.path.getmtime(os.path.join(path, "manifest.json")))
    return source_output


def sha256_file(path):
    h = hashlib.sha256()
    with open(path, "rb") as f:
        for chunk in iter(lambda: f.read(1 << 22), b""):
            h.update(chunk)
    return h.hexdigest()


def sha256(data):
    return hashlib.sha256(data).hexdigest()


def kiwad_crc(stored):
    """The CRC a KIWAD entry keeps: CRC-32 of the stored bytes with no initial or final inversion."""
    return zlib.crc32(stored, 0xFFFFFFFF) ^ 0xFFFFFFFF


class Wad:
    def __init__(self, path):
        self.path = path
        self.f = open(path, "rb")
        if self.f.read(5) != b"KIWAD":
            raise RuntimeError(f"{path} is not a KIWAD archive.")
        self.version, count = struct.unpack("<II", self.f.read(8))
        self.flag = self.f.read(1) if self.version >= 2 else b""
        self.entries = {}
        for _ in range(count):
            off, size, csize, comp, crc, nlen = struct.unpack("<IIIBII", self.f.read(21))
            name = self.f.read(nlen).rstrip(b"\0").decode("latin1")
            self.entries[name] = (off, size, csize, comp, crc)

    def read(self, name):
        off, size, csize, comp, _ = self.entries[name]
        self.f.seek(off)
        return zlib.decompress(self.f.read(csize)) if comp else self.f.read(size)

    def close(self):
        self.f.close()


def write_wad(source: Wad, replacements: dict, out_path):
    """source's entries with replacements (name -> bytes) put in, sorted by name as the game keeps them."""
    items = {name: ("orig", e) for name, e in source.entries.items()}
    for name, data in replacements.items():
        stored = zlib.compress(data, 9)
        items[name] = ("new", (data, stored))
    names = sorted(items, key=lambda s: s.encode("latin1"))
    header = 5 + 8 + len(source.flag) + sum(21 + len(n.encode("latin1")) + 1 for n in names)
    table, offset = [], header
    for name in names:
        kind, v = items[name]
        if kind == "orig":
            _, size, csize, comp, crc = v
            stored_len = csize if comp else size
            table.append((offset, size, csize, comp, crc, name))
        else:
            data, stored = v
            stored_len = len(stored)
            table.append((offset, len(data), len(stored), 1, kiwad_crc(stored), name))
        offset += stored_len
    if offset > 0xFFFFFFFF:
        raise RuntimeError("Root.wad would be larger than 4 GB.")

    tmp = out_path + ".tmp"
    with open(tmp, "wb") as w:
        w.write(b"KIWAD" + struct.pack("<II", source.version, len(names)) + source.flag)
        for off, size, csize, comp, crc, name in table:
            nb = name.encode("latin1") + b"\0"
            w.write(struct.pack("<IIIBII", off, size, csize, comp, crc, len(nb)) + nb)
        for name in names:
            kind, v = items[name]
            if kind == "orig":
                off, size, csize, comp, _ = v
                left = csize if comp else size
                source.f.seek(off)
                while left:
                    chunk = source.f.read(min(left, 1 << 22))
                    w.write(chunk)
                    left -= len(chunk)
            else:
                w.write(v[1])
    return tmp


def paths(game):
    return {
        "exe": os.path.join(game, "Bin", "WizardGraphicalClient.exe"),
        "wad": os.path.join(game, "Data", "GameData", "Root.wad"),
        "state": os.path.join(game, "Data", "GameData", STATE),
    }


def ensure_backup(path, check):
    """Keeps the original once. An existing backup must pass check (it is never overwritten)."""
    backup = path + BACKUP
    if os.path.exists(backup):
        check(backup)
        return backup
    check(path)
    with open(path, "rb") as src, open(backup + ".tmp", "wb") as dst:
        for chunk in iter(lambda: src.read(1 << 22), b""):
            dst.write(chunk)
    os.replace(backup + ".tmp", backup)
    print(f"  kept the original: {backup}")
    return backup


def install(game, generated):
    p = paths(game)
    manifest_path = os.path.join(generated, "manifest.json")
    if not os.path.exists(manifest_path):
        sys.exit(f"No {manifest_path}. Start the server once with [Mods] Enabled = ShadowSchool; it writes it.")
    with open(manifest_path, encoding="utf-8") as f:
        manifest = json.load(f)

    files = {}
    for entry in manifest["Files"]:
        src = os.path.join(generated, "root", *entry["Path"].split("/"))
        with open(src, "rb") as f:
            data = f.read()
        if sha256(data) != entry["Sha256"]:
            sys.exit(f"{src} does not match generated/manifest.json; start the server again to rebuild it.")
        files[entry["Path"]] = data

    print("Checking the game client...")

    def check_exe(path):
        with open(path, "rb") as f:
            exe = f.read()
        if exe_patch.is_patched(exe) or hashlib.sha256(exe).hexdigest() != exe_patch.SUPPORTED_SHA256:
            sys.exit(f"{path} is not the supported, unpatched client (r806919.Wizard_1_610). Nothing was changed.")

    def check_wad(path):
        wad = Wad(path)
        try:
            if any(n.startswith("Spells/ShadowSchool/") for n in wad.entries):
                sys.exit(f"{path} already holds Shadow School files, so it is not an original. Nothing was changed.")
            for entry in manifest["Files"]:
                if entry["ReplacesSha256"]:
                    name = entry["Path"]
                    if name not in wad.entries or sha256(wad.read(name)) != entry["ReplacesSha256"]:
                        sys.exit(f"{path}: {name} is not the file the server built the mod from (a different game "
                                 "revision?). Nothing was changed.")
        finally:
            wad.close()

    exe_backup = ensure_backup(p["exe"], check_exe)
    wad_backup = ensure_backup(p["wad"], check_wad)

    print(f"Writing Root.wad with {len(files)} Shadow School files...")
    source = Wad(wad_backup)
    try:
        tmp_wad = write_wad(source, files, p["wad"])
    finally:
        source.close()

    print("Patching WizardGraphicalClient.exe...")
    with open(exe_backup, "rb") as f:
        patched = exe_patch.patch(f.read())
    with open(p["exe"] + ".tmp", "wb") as f:
        f.write(patched)

    os.replace(tmp_wad, p["wad"])
    # Reinstalling updated Root.wad files while the game is open is useful for
    # content-only mod changes. The running client keeps the executable locked,
    # so avoid replacing it when it already has the exact patch we generated.
    if sha256_file(p["exe"]) == sha256(patched):
        os.remove(p["exe"] + ".tmp")
    else:
        os.replace(p["exe"] + ".tmp", p["exe"])
    state = {
        "mod": manifest["Mod"],
        "gameRevision": manifest["GameRevision"],
        "files": sorted(files),
        "rootWadSha256": sha256_file(p["wad"]),
        "exeSha256": sha256(patched),
    }
    with open(p["state"], "w", encoding="utf-8") as f:
        json.dump(state, f, indent=2)
    print("Installed. Uninstall with: python install.py uninstall")


def uninstall(game):
    p = paths(game)
    restored = False
    for key in ("exe", "wad"):
        backup = p[key] + BACKUP
        if os.path.exists(backup):
            os.replace(backup, p[key])
            print(f"  restored {p[key]}")
            restored = True
    if os.path.exists(p["state"]):
        os.remove(p["state"])
    print("Uninstalled." if restored else "Nothing to restore: no backups were found.")


def status(game):
    p = paths(game)
    if not os.path.exists(p["state"]):
        print("Shadow School is not installed in this client.")
        return
    with open(p["state"], encoding="utf-8") as f:
        state = json.load(f)
    exe_ok = sha256_file(p["exe"]) == state["exeSha256"]
    wad_ok = sha256_file(p["wad"]) == state["rootWadSha256"]
    print(f"Installed for {state['gameRevision']}: {len(state['files'])} Root.wad files.")
    print(f"  WizardGraphicalClient.exe: {'as installed' if exe_ok else 'CHANGED since install (patched by the launcher?)'}")
    print(f"  Root.wad: {'as installed' if wad_ok else 'CHANGED since install (patched by the launcher?)'}")

    generated = default_generated()
    manifest_path = os.path.join(generated, "manifest.json")
    if not os.path.exists(manifest_path):
        print("  Generated Shadow School resources: not found; start the server to build them.")
        return

    invalid, stale = generated_resource_drift(p["wad"], generated)

    if invalid:
        print(f"  Generated Shadow School resources: INVALID ({len(invalid)} file(s) disagree with manifest.json).")
    elif stale:
        print(f"  Generated Shadow School resources: UPDATE AVAILABLE ({len(stale)} Root.wad file(s) differ).")
        for name in stale[:5]:
            print(f"    {name}")
        if len(stale) > 5:
            print(f"    ... and {len(stale) - 5} more")
        print("  Run install.py install to update the client while preserving the recorded original backups.")
    else:
        print("  Generated Shadow School resources: current.")


def generated_resource_drift(wad_path, generated):
    """Compare the latest generated files with the installed Root.wad without modifying either."""
    manifest_path = os.path.join(generated, "manifest.json")
    with open(manifest_path, encoding="utf-8") as f:
        manifest = json.load(f)
    wad = Wad(wad_path)
    stale = []
    invalid = []
    try:
        for entry in manifest["Files"]:
            name = entry["Path"]
            generated_file = os.path.join(generated, "root", *name.split("/"))
            try:
                with open(generated_file, "rb") as f:
                    data = f.read()
            except OSError:
                invalid.append(name)
                continue
            if sha256(data) != entry["Sha256"]:
                invalid.append(name)
            elif name not in wad.entries or sha256(wad.read(name)) != entry["Sha256"]:
                stale.append(name)
    finally:
        wad.close()

    return invalid, stale


def main():
    parser = argparse.ArgumentParser(description="Shadow School client installer")
    parser.add_argument("command", choices=["install", "uninstall", "status"])
    parser.add_argument("--game", default=DEFAULT_GAME, help="the Wizard101 folder (holds Bin and Data)")
    parser.add_argument("--generated", default=default_generated(),
                        help="the mod's generated folder written by the server")
    args = parser.parse_args()
    if args.command == "install":
        install(args.game, args.generated)
    elif args.command == "uninstall":
        uninstall(args.game)
    else:
        status(args.game)


if __name__ == "__main__":
    main()
