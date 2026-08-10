#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[2]
errors = []
for name in ('Assets', 'Packages', 'ProjectSettings'):
    if not (ROOT / name).is_dir():
        errors.append(f'root Unity project is missing {name}/')
if not (ROOT / 'ProjectSettings' / 'ProjectVersion.txt').is_file():
    errors.append('root Unity project is missing ProjectSettings/ProjectVersion.txt')

nested = []
for version_file in ROOT.rglob('ProjectSettings/ProjectVersion.txt'):
    project_root = version_file.parent.parent
    if project_root != ROOT and (project_root / 'Assets').is_dir() and (project_root / 'Packages').is_dir():
        nested.append(project_root.relative_to(ROOT))
if nested:
    errors.append('secondary Unity project(s) detected: ' + ', '.join(map(str, sorted(nested))))

legacy = ROOT / 'unity' / 'Dig.Unity'
if legacy.exists():
    errors.append('legacy secondary Unity project must not exist')

if errors:
    print('Unity project layout check failed:', file=sys.stderr)
    for error in errors:
        print(f'- {error}', file=sys.stderr)
    raise SystemExit(1)
print('PASS: repository root is the only Unity project')
