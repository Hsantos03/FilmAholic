# -*- coding: utf-8 -*-
path = r"c:\Users\crist\Desktop\projetoEsa\FilmAholic\filmaholic.client\src\app\components\admin-panel\admin-panel.component.html"
with open(path, "r", encoding="utf-8") as f:
    s = f.read()

r1 = '<span class="admin-tab__ico" aria-hidden="true"><svg class="fi-icon" viewBox="0 0 24 24"><path d="M12 12c2.21 0 4-1.79 4-4s-1.79-4-4-4-4 1.79-4 4 1.79 4 4 4zm0 2c-2.67 0-8 1.34-8 4v2h16v-2c0-2.66-5.33-4-8-4z"/></svg></span>'
r2 = '<span class="admin-tab__ico" aria-hidden="true"><svg class="fi-icon" viewBox="0 0 24 24"><path d="M17 11V3H7v8H4v12h6v-6h2v6h6V11h-1zM9 5h6v6H9V5z"/></svg></span>'
r3 = '<span class="admin-tab__ico" aria-hidden="true"><svg class="fi-icon" viewBox="0 0 24 24"><path d="M3 9v6h4l5 4V5L7 9H3zm13.5 3c0-1.77-1.02-3.29-2.5-4.03v8.05c1.48-.73 2.5-2.25 2.5-4.02zM14 9.23v5.54c1.07-.59 1.78-1.74 1.78-2.77 0-1.03-.71-1.18-1.78-1.77z"/></svg></span>'
r4 = '<span class="admin-tab__ico" aria-hidden="true"><svg class="fi-icon" viewBox="0 0 24 24"><path d="M19 5h-2V3H7v2H5c-1.1 0-2 .9-2 2v1c0 2.55 1.92 4.63 4.39 4.94.63 1.5 1.98 2.63 3.61 2.96V19H7v2h10v-2h-4v-3.1c1.63-.33 2.98-1.46 3.61-2.96C19.08 12.63 21 10.55 21 8V7c0-1.1-.9-2-2-2zM5 8V7h2v3.82C5.84 10.4 5 9.3 5 8zm14 0c0 1.3-.84 2.4-2 2.82V7h2v1z"/></svg></span>'
r5 = '<span class="admin-empty__icon" aria-hidden="true"><svg class="fi-icon" viewBox="0 0 24 24"><path d="M15.5 14h-.79l-.28-.27A6.471 6.471 0 0 0 16 9.5 6.5 6.5 0 1 0 9.5 16c1.61 0 3.09-.59 4.23-1.57l.27.28v.79l5 4.99L20.49 19l-4.99-5zm-6 0C7.01 14 5 11.99 5 9.5S7.01 5 9.5 5 14 7.01 14 9.5 11.99 14 9.5 14z"/></svg></span>'

pairs = [
    ('<span class="admin-tab__ico" aria-hidden="true">\U0001f464</span>', r1),
    ('<span class="admin-tab__ico" aria-hidden="true">\U0001f3d8\ufe0f</span>', r2),
    ('<span class="admin-tab__ico" aria-hidden="true">\U0001f4e3</span>', r3),
    ('<span class="admin-tab__ico" aria-hidden="true">\U0001f3c5</span>', r4),
    ('<span class="admin-empty__icon" aria-hidden="true">\U0001f50d</span>', r5),
]
for old, new in pairs:
    if old not in s:
        raise SystemExit(f"missing: {old!r}")
    s = s.replace(old, new, 1)

with open(path, "w", encoding="utf-8", newline="\n") as f:
    f.write(s)
print("ok")
