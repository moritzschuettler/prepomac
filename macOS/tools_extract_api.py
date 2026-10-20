#!/usr/bin/env python3
"""Extracts the public members of a C# class and writes a partial class with logging stubs (macOS port scaffolding)."""
import re, sys
src = open(sys.argv[1], encoding='utf-8-sig').read()
cls = sys.argv[2]
out = sys.argv[3]
skip = set(sys.argv[4].split(',')) if len(sys.argv) > 4 else set()
lines = src.split('\n')
# find class start
start = next(i for i, l in enumerate(lines) if re.search(r'\bclass\s+' + cls + r'\b', l))
depth = 0
i = start
members = []
in_class = False
while i < len(lines):
    l = lines[i]
    code = re.sub(r'//.*', '', l)
    code = re.sub(r'"(?:\\.|[^"\\])*"', '""', code)
    if depth == 1 and re.match(r'\s*public\s', code) and not re.match(r'\s*public\s+(partial\s+)?(class|struct|enum|interface)\b', code):
        # collect declaration until { or ; or =>
        decl = code.strip()
        j = i
        while not re.search(r'[{;]|=>', decl):
            j += 1
            decl += ' ' + re.sub(r'//.*', '', lines[j]).strip()
        members.append((i, decl))
    for ch in code:
        if ch == '{':
            depth += 1
            in_class = True
        elif ch == '}':
            depth -= 1
    if in_class and depth == 0:
        break
    i += 1
def stub(decl):
    head = re.split(r'\{|=>', decl)[0].strip()
    if head.endswith(';'):
        return '        ' + head  # field or constant
    m = re.match(r'public\s+(?:static\s+|virtual\s+|override\s+|new\s+)*(.*?)\s*([A-Za-z_][A-Za-z0-9_]*)\s*\((.*)\)\s*$', head)
    if m:
        ret, name, params = m.group(1), m.group(2), m.group(3)
        if name in skip or name == cls: return None
        outs = re.findall(r'\bout\s+[\w\.<>\[\], ]+?\s+(\w+)\s*(?:,|$)', params)
        body = ''.join('%s = default; ' % o for o in outs)
        body += 'NotImplemented("%s"); ' % name
        if ret not in ('void', ''):
            body += 'return default; '
        return '        %s { %s}' % (head, body)
    # property or event
    if re.match(r'public\s+event\s', head):
        return '        ' + head + ';'
    m = re.match(r'public\s+(?:static\s+|virtual\s+|override\s+|new\s+)*(.*?)\s+([A-Za-z_][A-Za-z0-9_]*)$', head)
    if m:
        name = m.group(2)
        if name in skip: return None
        return '        %s { get; set; }' % head
    return '        // ' + head
result = ['// Generated scaffolding of the public members of ' + cls + ' (macOS); the members are implemented in the other files', '']
for _, d in members:
    s = stub(d)
    if s: result.append(s)
open(out, 'w').write('\n'.join(result) + '\n')
print(len(members), 'members')
