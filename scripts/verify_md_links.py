#!/usr/bin/env python3
import os, re, sys, urllib.parse

root = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
skip_dirs = {'.git', 'node_modules', '__pycache__', '.venv', 'venv', '.idea'}
md_files = []
for dirpath, dirnames, filenames in os.walk(root):
    parts = dirpath.split(os.sep)
    if any(p in skip_dirs for p in parts):
        continue
    for f in filenames:
        if f.lower().endswith('.md'):
            md_files.append(os.path.join(dirpath, f))

inline_re = re.compile(r'\[([^\]]+)\]\(([^)]+)\)')
ref_def_re = re.compile(r'^\s{0,3}\[([^\]]+)\]:\s*(\S+)', re.MULTILINE)
ref_use_re = re.compile(r'\[([^\]]+)\]\[([^\]]*)\]')

broken = []
checked = 0
for md in md_files:
    try:
        with open(md, 'r', encoding='utf-8') as fh:
            text = fh.read()
    except Exception as e:
        broken.append({'file': md, 'line': 0, 'link': '<file-read-error>', 'target': str(e), 'resolved': ''})
        continue
    defs = {m.group(1).lower(): m.group(2) for m in ref_def_re.finditer(text)}
    lines = text.splitlines()
    for i, line in enumerate(lines, start=1):
        for m in inline_re.finditer(line):
            url = m.group(2).strip()
            if url.startswith(('http://','https://','mailto:','tel:','#')):
                continue
            url = urllib.parse.unquote(url)
            if url.startswith('<') and url.endswith('>'):
                url = url[1:-1]
            if '#' in url:
                url_path = url.split('#', 1)[0]
            else:
                url_path = url
            if url_path == '':
                continue
            if url_path.startswith('/'):
                candidate = os.path.join(root, url_path.lstrip('/'))
            elif os.path.isabs(url_path):
                candidate = url_path
            else:
                candidate = os.path.normpath(os.path.join(os.path.dirname(md), url_path))
            alt_candidate = os.path.normpath(os.path.join(root, url_path))
            exists = os.path.exists(candidate) or os.path.exists(alt_candidate)
            if not exists:
                if not os.path.splitext(candidate)[1]:
                    if os.path.exists(candidate + '.md') or os.path.exists(alt_candidate + '.md') or os.path.exists(os.path.join(candidate, 'README.md')) or os.path.exists(os.path.join(alt_candidate, 'README.md')):
                        exists = True
            checked += 1
            if not exists:
                broken.append({'file': md, 'line': i, 'link': m.group(0), 'target': url, 'resolved': candidate})
        for m in ref_use_re.finditer(line):
            ref = m.group(2).strip().lower()
            if ref == '':
                ref = m.group(1).strip().lower()
            if ref in defs:
                url = defs[ref]
                if url.startswith(('http://','https://','mailto:','tel:','#')):
                    continue
                url = urllib.parse.unquote(url)
                if '#' in url:
                    url_path = url.split('#', 1)[0]
                else:
                    url_path = url
                if url_path == '':
                    continue
                if url_path.startswith('/'):
                    candidate = os.path.join(root, url_path.lstrip('/'))
                elif os.path.isabs(url_path):
                    candidate = url_path
                else:
                    candidate = os.path.normpath(os.path.join(os.path.dirname(md), url_path))
                alt_candidate = os.path.normpath(os.path.join(root, url_path))
                exists = os.path.exists(candidate) or os.path.exists(alt_candidate)
                if not exists:
                    if not os.path.splitext(candidate)[1]:
                        if os.path.exists(candidate + '.md') or os.path.exists(alt_candidate + '.md') or os.path.exists(os.path.join(candidate, 'README.md')) or os.path.exists(os.path.join(alt_candidate, 'README.md')):
                            exists = True
                checked += 1
                if not exists:
                    broken.append({'file': md, 'line': i, 'link': m.group(0), 'target': url, 'resolved': candidate})

report_dir = os.path.join(root, 'docs', 'project', '02-as-is-audit')
os.makedirs(report_dir, exist_ok=True)
report_path = os.path.join(report_dir, '12-FASE-0-VERIFICATION.md')
with open(report_path, 'w', encoding='utf-8') as out:
    out.write('# Fase 0 — Verificación de Enlaces Markdown\n\n')
    out.write('**Fecha:** 2026-04-01\n\n')
    out.write('Resumen:\n\n')
    out.write(f'- Archivos MD analizados: {len(md_files)}\n')
    out.write(f'- Links verificados (aprox): {checked}\n')
    out.write(f'- Links rotos encontrados: {len(broken)}\n\n')
    if broken:
        out.write('## Links rotos\n\n')
        for b in broken:
            out.write(f'- File: {b.get("file")} (line {b.get("line")})\n  - Link: {b.get("link")} -> Target: {b.get("target")}\n  - Resolved path tried: {b.get("resolved")}\n\n')
    else:
        out.write('No se encontraron links rotos.\n\n')

print(f'Checked {len(md_files)} files, {checked} links, {len(broken)} broken. Report written to {report_path}')
