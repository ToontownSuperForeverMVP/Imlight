import hashlib
import importlib.util
import json
import pathlib
import sys
import tempfile
import unittest
from unittest.mock import patch


REPO = pathlib.Path(__file__).resolve().parents[2]
INSTALLER_PATH = REPO / "src/Imlight.Director/mods/ShadowSchool/client/install.py"
SPEC = importlib.util.spec_from_file_location("shadow_school_install", INSTALLER_PATH)
INSTALLER = importlib.util.module_from_spec(SPEC)
sys.modules[SPEC.name] = INSTALLER
SPEC.loader.exec_module(INSTALLER)


class FakeWad:
    def __init__(self, _path, entries):
        self.entries = entries

    def read(self, name):
        return self.entries[name]

    def close(self):
        pass


class GeneratedResourceDriftTests(unittest.TestCase):
    def test_reports_installed_locale_stale_after_generated_update(self):
        installed = b"old ShadowSchool locale"
        generated = b"C1 enrollment and check-in strings"
        name = "Locale/en-US/ShadowSchool.lang"

        with tempfile.TemporaryDirectory() as folder:
            root = pathlib.Path(folder)
            generated_root = root / "generated/root" / "Locale/en-US"
            generated_root.mkdir(parents=True)
            (root / "generated/manifest.json").write_text(json.dumps({
                "Files": [{"Path": name, "Sha256": hashlib.sha256(generated).hexdigest()}]
            }), encoding="utf-8")
            (generated_root / "ShadowSchool.lang").write_bytes(generated)
            fake_wad = FakeWad("unused", {name: installed})

            with patch.object(INSTALLER, "Wad", return_value=fake_wad):
                invalid, stale = INSTALLER.generated_resource_drift("unused", str(root / "generated"))

        self.assertEqual([], invalid)
        self.assertEqual([name], stale)

    def test_reports_current_when_installed_locale_matches_generated_manifest(self):
        current = b"C1 enrollment and check-in strings"
        name = "Locale/en-US/ShadowSchool.lang"

        with tempfile.TemporaryDirectory() as folder:
            root = pathlib.Path(folder)
            generated_root = root / "generated/root" / "Locale/en-US"
            generated_root.mkdir(parents=True)
            (root / "generated/manifest.json").write_text(json.dumps({
                "Files": [{"Path": name, "Sha256": hashlib.sha256(current).hexdigest()}]
            }), encoding="utf-8")
            (generated_root / "ShadowSchool.lang").write_bytes(current)
            fake_wad = FakeWad("unused", {name: current})

            with patch.object(INSTALLER, "Wad", return_value=fake_wad):
                invalid, stale = INSTALLER.generated_resource_drift("unused", str(root / "generated"))

        self.assertEqual([], invalid)
        self.assertEqual([], stale)


if __name__ == "__main__":
    unittest.main()
