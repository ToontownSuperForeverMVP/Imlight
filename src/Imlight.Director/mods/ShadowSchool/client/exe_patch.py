"""
Shadow School client adapter: the eighth school button on the character creation school page.

The school page (GUI/CreationClassSelect.gui, ClassSelectWindow in WizardGraphicalClient.exe) knows seven buttons by
name and keeps the chosen school as an index 0..6 at this+0x57C. On OK the index is looked up in the client's global
school list (Fire, Ice, Storm, Myth, Life, Death, Balance, Star, Sun, Moon, Gardening, Shadow, ...) and that school's
id goes into the creation request. Shadow is already in that list (id 1429009101), so no school is invented here.

The patch adds a code section (.shdw) and seven hooks:
  CLICK   ShadowButton chooses index 7, an index for this page only (its own tables have seven entries).
  OK      index 7 becomes the position of school id 1429009101 in the global school list.
  HEADER  the school's title art is skipped for index 7 (the game has no Shadow title art).
  SIGIL   the school's sigil; for index 7 the GUI's ShadowSigil sprite instead. ShadowButton is hidden.
  COLOR   the description text colour; for index 7 a fixed colour instead of reading past the table.
  QUIZ    showing a quiz question hides ShadowSigil and ShadowButton.
  LIST    showing the pick-your-school list hides ShadowSigil and shows ShadowButton.

Only this exact client build is patched: the file's SHA-256 and the original bytes of every hook site are checked
first. install.py keeps a backup and restores it on uninstall.
"""

import hashlib
import struct

# r806919.Wizard_1_610, WizardGraphicalClient.exe of 2026-09-16.
SUPPORTED_SHA256 = "544ab726ef7b920a5ccb67aa3d06b245db7e0012317d2fe7856d58c17c31cf7e"
IMAGE_BASE = 0x140000000
SECTION_NAME = b".shdw\0\0\0"
SHADOW_SCHOOL_ID = 1429009101

BUTTON_NAME = b"ShadowButton"
SIGIL_NAME = b"ShadowSigil"

# Functions and data of the client used by the hooks (static addresses of this build).
FIND_CHILD = 0x14181BC70        # Window* Window::FindChild(this, const std::string& name), searches children too
SHOW_RESULT = 0x1407DC5D0       # ClassSelectWindow: show the chosen school (rcx = this)
CLICK_DONE = 0x1407DDB39        # end of the click handler's name compares
BALANCE_CHECK = 0x1407DDAC1     # the SelectBalanceButton compare, right after "mov r8, rbp"
SCHOOL_LIST = 0x143336558       # the global school list (vector of 16-byte entries at +0x18..+0x20, id at +0x80)
COLOR_TABLE = 0x1433415A8       # int[7] description text colours
SHADOW_COLOR = 0xFF9B7BC9       # description text colour for Shadow
VTBL_SHOW = 0x170               # Window::ShowWindow(bool) in the window vtable

# Hook sites: name, address, original bytes, whether it is entered with a call (returns) or a jmp.
SITES = [
    ("CLICK", 0x1407DDAB8, "4883fd13757b4c8bc5", "jmp"),
    ("OK", 0x1407DD7F8, "448b877c050000", "call"),
    ("HEADER", 0x1407DCAD9, "4863867c050000488b8cc628040000488b01b201ff9070010000", "call"),
    ("SIGIL", 0x1407DCB05, "4863867c050000488b8cc6f0030000488b01b201ff9070010000", "call"),
    ("COLOR", 0x1407DD0A7, "4863867c050000488d0df344b6028b1c81", "call"),
    ("QUIZ", 0x1407DB570, "48895c2410", "jmp"),
    ("LIST", 0x1407DDB70, "48895c2410", "jmp"),
]


class Asm:
    """Bytes, labels, and rel32/rel8 fixups to labels or absolute (static) addresses."""

    def __init__(self, base):
        self.base = base
        self.buf = bytearray()
        self.labels = {}
        self.fixups = []

    def label(self, name):
        self.labels[name] = self.base + len(self.buf)

    def raw(self, hexstr):
        self.buf += bytes.fromhex(hexstr.replace(" ", ""))

    def data(self, b):
        self.buf += b

    def rel32(self, prefix, target):
        self.raw(prefix)
        self.fixups.append((4, len(self.buf), target))
        self.buf += b"\0" * 4

    def rel8(self, prefix, target):
        self.raw(prefix)
        self.fixups.append((1, len(self.buf), target))
        self.buf += b"\0"

    def align(self, n=16):
        while len(self.buf) % n:
            self.buf += b"\xcc"

    def resolve(self):
        for size, pos, target in self.fixups:
            dest = self.labels[target] if isinstance(target, str) else target
            d = dest - (self.base + pos + size)
            if size == 1:
                assert -128 <= d <= 127, target
                struct.pack_into("<b", self.buf, pos, d)
            else:
                struct.pack_into("<i", self.buf, pos, d)
        return bytes(self.buf)


def sso_string(text):
    """An MSVC std::string of at most 15 characters, stored inline: no pointer, nothing to free."""
    assert len(text) <= 15
    return text.ljust(16, b"\0") + struct.pack("<QQ", len(text), 15)


def build_cave(base):
    a = Asm(base)
    site = {name: addr for name, addr, _, _ in SITES}

    # SHOW(rcx = window, rdx = &std::string name, r8b = show): find the child by name and show or hide it.
    a.label("SHOW")
    a.raw("53")                       # push rbx
    a.raw("4883ec20")                 # sub rsp, 0x20
    a.raw("410fb6d8")                 # movzx ebx, r8b
    a.rel32("e8", FIND_CHILD)
    a.raw("4885c0")                   # test rax, rax
    a.raw("740e")                     # jz done
    a.raw("488bc8")                   # mov rcx, rax
    a.raw("488b00")                   # mov rax, [rax]
    a.raw("88da")                     # mov dl, bl
    a.raw("ff90"); a.data(struct.pack("<I", VTBL_SHOW))   # call [rax + 0x170]
    a.raw("4883c420")                 # done: add rsp, 0x20
    a.raw("5b")                       # pop rbx
    a.raw("c3")
    a.align()

    # CLICK replaces "cmp rbp, 0x13 / jne done / mov r8, rbp". rbx = clicked name, rbp = its length, rdi = this.
    a.label("CLICK")
    a.raw("4883fd13")                 # cmp rbp, 0x13
    a.rel8("74", "CLICK_BALANCE")
    a.raw("4883fd%02x" % len(BUTTON_NAME))   # cmp rbp, len("ShadowButton")
    a.rel8("75", "CLICK_DONE")
    a.raw("48b8"); a.data(BUTTON_NAME[:8])   # mov rax, "ShadowBu"
    a.raw("483903")                   # cmp [rbx], rax
    a.rel8("75", "CLICK_DONE")
    a.raw("817b08"); a.data(BUTTON_NAME[8:12])   # cmp dword [rbx + 8], "tton"
    a.rel8("75", "CLICK_DONE")
    a.raw("c7877c05000007000000")     # mov dword [rdi + 0x57c], 7
    a.raw("c6878405000001")           # mov byte [rdi + 0x584], 1  (as the other school buttons do)
    a.raw("488bcf")                   # mov rcx, rdi
    a.rel32("e8", SHOW_RESULT)
    a.label("CLICK_DONE")
    a.rel32("e9", CLICK_DONE)
    a.label("CLICK_BALANCE")
    a.raw("4c8bc5")                   # mov r8, rbp
    a.rel32("e9", BALANCE_CHECK)
    a.align()

    # OK replaces "mov r8d, [rdi + 0x57c]": the position of the chosen school in the global school list.
    a.label("OK")
    a.raw("448b877c050000")           # mov r8d, [rdi + 0x57c]
    a.raw("4183f807")                 # cmp r8d, 7
    a.rel8("75", "OK_RET")
    a.rel32("488b05", SCHOOL_LIST)    # mov rax, [school list]
    a.raw("4c8b4818")                 # mov r9, [rax + 0x18]
    a.raw("4c8b5020")                 # mov r10, [rax + 0x20]
    a.raw("4531c0")                   # xor r8d, r8d
    a.label("OK_LOOP")
    a.raw("4d39d1")                   # cmp r9, r10
    a.rel8("73", "OK_NONE")           # jae
    a.raw("498b01")                   # mov rax, [r9]
    a.raw("81b880000000"); a.data(struct.pack("<I", SHADOW_SCHOOL_ID))   # cmp dword [rax + 0x80], id
    a.rel8("74", "OK_RET")
    a.raw("4983c110")                 # add r9, 16
    a.raw("41ffc0")                   # inc r8d
    a.rel8("eb", "OK_LOOP")
    a.label("OK_NONE")
    a.raw("41b8ffffff7f")             # mov r8d, 0x7fffffff: past the end, the lookup gives no school
    a.label("OK_RET")
    a.raw("c3")
    a.align()

    # HEADER: show the chosen school's title art; Shadow has none. rsi = this.
    a.label("HEADER")
    a.raw("4883ec28")                 # sub rsp, 0x28
    a.raw("4863867c050000")           # movsxd rax, [rsi + 0x57c]
    a.raw("4883f807")                 # cmp rax, 7
    a.rel8("7d", "HEADER_END")        # jge
    a.raw("488b8cc628040000")         # mov rcx, [rsi + rax*8 + 0x428]
    a.raw("488b01")                   # mov rax, [rcx]
    a.raw("b201")                     # mov dl, 1
    a.raw("ff9070010000")             # call [rax + 0x170]
    a.label("HEADER_END")
    a.raw("4883c428")                 # add rsp, 0x28
    a.raw("c3")
    a.align()

    # SIGIL: show the chosen school's sigil, ShadowSigil only for Shadow; hide ShadowButton. rsi = this.
    a.label("SIGIL")
    a.raw("4883ec28")                 # sub rsp, 0x28
    a.raw("4863867c050000")           # movsxd rax, [rsi + 0x57c]
    a.raw("4883f807")                 # cmp rax, 7
    a.rel8("7d", "SIGIL_OWN")         # jge
    a.raw("488b8cc6f0030000")         # mov rcx, [rsi + rax*8 + 0x3f0]
    a.raw("488b01")                   # mov rax, [rcx]
    a.raw("b201")                     # mov dl, 1
    a.raw("ff9070010000")             # call [rax + 0x170]
    a.label("SIGIL_OWN")
    a.raw("4863867c050000")           # movsxd rax, [rsi + 0x57c]
    a.raw("4883f807")                 # cmp rax, 7
    a.raw("0f9dc0")                   # setge al
    a.raw("440fb6c0")                 # movzx r8d, al
    a.raw("488bce")                   # mov rcx, rsi
    a.rel32("488d15", "STR_SIGIL")    # lea rdx, [ShadowSigil]
    a.rel32("e8", "SHOW")
    a.raw("488bce")                   # mov rcx, rsi
    a.rel32("488d15", "STR_BUTTON")   # lea rdx, [ShadowButton]
    a.raw("4531c0")                   # xor r8d, r8d
    a.rel32("e8", "SHOW")
    a.raw("4883c428")                 # add rsp, 0x28
    a.raw("c3")
    a.align()

    # COLOR replaces the colour table read: ebx = the description text colour. rsi = this.
    a.label("COLOR")
    a.raw("4863867c050000")           # movsxd rax, [rsi + 0x57c]
    a.raw("4883f807")                 # cmp rax, 7
    a.rel8("7c", "COLOR_TABLE")       # jl
    a.raw("bb"); a.data(struct.pack("<I", SHADOW_COLOR))   # mov ebx, colour
    a.raw("c3")
    a.label("COLOR_TABLE")
    a.rel32("488d0d", COLOR_TABLE)    # lea rcx, [table]
    a.raw("8b1c81")                   # mov ebx, [rcx + rax*4]
    a.raw("c3")
    a.align()

    # QUIZ / LIST: function entries (rcx = this). The displaced first instruction runs first, then back.
    for name, show_button in (("QUIZ", 0), ("LIST", 1)):
        a.label(name)
        a.raw("48895c2410")           # mov [rsp + 0x10], rbx
        a.raw("51 52 4150 4151")      # push rcx, rdx, r8, r9
        a.raw("4883ec28")             # sub rsp, 0x28
        a.raw("488b4c2440")           # mov rcx, [rsp + 0x40]  (this)
        a.rel32("488d15", "STR_SIGIL")
        a.raw("4531c0")               # xor r8d, r8d
        a.rel32("e8", "SHOW")
        a.raw("488b4c2440")           # mov rcx, [rsp + 0x40]
        a.rel32("488d15", "STR_BUTTON")
        a.raw("41b0%02x" % show_button)   # mov r8b, show
        a.rel32("e8", "SHOW")
        a.raw("4883c428")             # add rsp, 0x28
        a.raw("4159 4158 5a 59")      # pop r9, r8, rdx, rcx
        a.rel32("e9", site[name] + 5)
        a.align()

    a.label("STR_SIGIL")
    a.data(sso_string(SIGIL_NAME))
    a.label("STR_BUTTON")
    a.data(sso_string(BUTTON_NAME))
    return a


def _section_layout(pe):
    e_lfanew = struct.unpack_from("<I", pe, 0x3C)[0]
    nsec = struct.unpack_from("<H", pe, e_lfanew + 6)[0]
    opt_size = struct.unpack_from("<H", pe, e_lfanew + 20)[0]
    opt = e_lfanew + 24
    return e_lfanew, nsec, opt, opt + opt_size


def _va_to_offset(pe, va):
    _, nsec, _, table = _section_layout(pe)
    rva = va - IMAGE_BASE
    for i in range(nsec):
        vaddr, rawsize, rawptr = struct.unpack_from("<III", pe, table + 40 * i + 12)
        if vaddr <= rva < vaddr + rawsize:
            return rawptr + (rva - vaddr)
    raise ValueError(hex(va))


def is_patched(exe: bytes) -> bool:
    _, nsec, _, table = _section_layout(exe)
    return any(exe[table + 40 * i:table + 40 * i + 8] == SECTION_NAME for i in range(nsec))


def patch(original: bytes) -> bytes:
    if hashlib.sha256(original).hexdigest() != SUPPORTED_SHA256:
        raise RuntimeError("This WizardGraphicalClient.exe is not the supported build "
                           "(r806919.Wizard_1_610); nothing was changed.")

    pe = bytearray(original)
    e_lfanew, nsec, opt, table = _section_layout(pe)
    for name, va, orig, _ in SITES:
        off = _va_to_offset(pe, va)
        if pe[off:off + len(orig) // 2].hex() != orig:
            raise RuntimeError(f"Hook site {name} does not hold the expected bytes; nothing was changed.")

    sect_align, file_align = struct.unpack_from("<II", pe, opt + 32)
    size_of_image, size_of_headers = struct.unpack_from("<II", pe, opt + 56)
    if table + 40 * (nsec + 1) > size_of_headers:
        raise RuntimeError("There is no room for another section header; nothing was changed.")

    # The Authenticode signature (data directory 4) is the end of the file. A changed file cannot keep it.
    cert_dir = opt + 112 + 8 * 4
    cert_off, cert_size = struct.unpack_from("<II", pe, cert_dir)
    if cert_size:
        if cert_off + cert_size != len(pe):
            raise RuntimeError("The signature is not at the end of the file; nothing was changed.")
        del pe[cert_off:]
        struct.pack_into("<II", pe, cert_dir, 0, 0)

    raw_ptr = -(-len(pe) // file_align) * file_align
    pe += b"\0" * (raw_ptr - len(pe))
    rva = size_of_image
    asm = build_cave(IMAGE_BASE + rva)
    code = asm.resolve()
    raw_size = -(-len(code) // file_align) * file_align
    pe += code + b"\0" * (raw_size - len(code))

    # IMAGE_SCN_CNT_CODE | MEM_EXECUTE | MEM_READ. Everything in it is position independent (rip-relative).
    struct.pack_into("<8sIIIIIIHHI", pe, table + 40 * nsec,
                     SECTION_NAME, len(code), rva, raw_size, raw_ptr, 0, 0, 0, 0, 0x60000020)
    struct.pack_into("<H", pe, e_lfanew + 6, nsec + 1)
    struct.pack_into("<I", pe, opt + 56, rva + -(-len(code) // sect_align) * sect_align)
    struct.pack_into("<I", pe, opt + 64, 0)   # CheckSum is not checked for applications

    for name, va, orig, kind in SITES:
        off = _va_to_offset(pe, va)
        n = len(orig) // 2
        op = b"\xe8" if kind == "call" else b"\xe9"
        pe[off:off + n] = op + struct.pack("<i", asm.labels[name] - (va + 5)) + b"\x90" * (n - 5)
    return bytes(pe)
