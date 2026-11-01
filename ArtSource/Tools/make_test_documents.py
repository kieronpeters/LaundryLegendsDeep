import os, zipfile, datetime

OUT = os.path.join("TestResults", "generated")
os.makedirs(OUT, exist_ok=True)

TITLE = "Laundry Legends - Daily Summary (test document)"
LINES = [
    "Date: 2026-11-01",
    "Location: Bali neighbourhood laundromat",
    "",
    "Orders completed: 3",
    "Income: Rp 120.000",
    "Tips: Rp 30.000",
    "Daily overhead: Rp 15.000",
    "Net: Rp 135.000",
    "",
    "Arcade plays: 4",
    "Upgrades owned: Fast Dryer",
    "",
    "This file exists to verify that documents can be generated on request.",
]

def esc(text):
    return text.replace("&", "&amp;").replace("<", "&lt;").replace(">", "&gt;")

# ---------------------------------------------------------------- DOCX
doc_paragraphs = [f'<w:p><w:r><w:rPr><w:b/><w:sz w:val="32"/></w:rPr><w:t>{esc(TITLE)}</w:t></w:r></w:p>']
for line in LINES:
    doc_paragraphs.append(f'<w:p><w:r><w:t xml:space="preserve">{esc(line)}</w:t></w:r></w:p>')

document = f'''<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<w:document xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main">
<w:body>{''.join(doc_paragraphs)}<w:sectPr/></w:body></w:document>'''

content_types = '''<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">
<Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/>
<Default Extension="xml" ContentType="application/xml"/>
<Override PartName="/word/document.xml" ContentType="application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml"/>
</Types>'''

rels = '''<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
<Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="word/document.xml"/>
</Relationships>'''

docx_path = os.path.join(OUT, "kikie-test.docx")
with zipfile.ZipFile(docx_path, "w", zipfile.ZIP_DEFLATED) as z:
    z.writestr("[Content_Types].xml", content_types)
    z.writestr("_rels/.rels", rels)
    z.writestr("word/document.xml", document)

# ---------------------------------------------------------------- PDF
def pdf_escape(text):
    return text.replace("\\", r"\\").replace("(", r"\(").replace(")", r"\)")

content = "BT\n/F1 16 Tf\n60 780 Td\n(" + pdf_escape(TITLE) + ") Tj\n/F1 11 Tf\n0 -28 Td\n"
for line in LINES:
    content += f"({pdf_escape(line)}) Tj\n0 -16 Td\n"
content += "ET"

objects = []
objects.append("<< /Type /Catalog /Pages 2 0 R >>")
objects.append("<< /Type /Pages /Kids [3 0 R] /Count 1 >>")
objects.append("<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] "
               "/Resources << /Font << /F1 5 0 R >> >> /Contents 4 0 R >>")
objects.append(f"<< /Length {len(content.encode('latin-1'))} >>\nstream\n{content}\nendstream")
objects.append("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>")

pdf = bytearray(b"%PDF-1.4\n")
offsets = []
for index, body in enumerate(objects, start=1):
    offsets.append(len(pdf))
    pdf += f"{index} 0 obj\n{body}\nendobj\n".encode("latin-1")

xref_at = len(pdf)
pdf += f"xref\n0 {len(objects) + 1}\n0000000000 65535 f \n".encode("latin-1")
for offset in offsets:
    pdf += f"{offset:010d} 00000 n \n".encode("latin-1")
pdf += (f"trailer\n<< /Size {len(objects) + 1} /Root 1 0 R >>\n"
        f"startxref\n{xref_at}\n%%EOF\n").encode("latin-1")

pdf_path = os.path.join(OUT, "kikie-test.pdf")
with open(pdf_path, "wb") as handle:
    handle.write(pdf)

print("DOCX", os.path.getsize(docx_path), "bytes")
print("PDF ", os.path.getsize(pdf_path), "bytes")

with zipfile.ZipFile(docx_path) as z:
    print("DOCX entries:", z.namelist())
    print("DOCX testzip:", z.testzip())

data = open(pdf_path, "rb").read()
print("PDF header:", data[:8])
print("PDF has EOF:", data.rstrip().endswith(b"%%EOF"))
