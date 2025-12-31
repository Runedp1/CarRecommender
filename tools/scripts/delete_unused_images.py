"""
Script om ongebruikte images te verwijderen.
Leest images_to_delete.txt en verwijdert de bestanden.
"""
import os
import sys

script_dir = os.path.dirname(os.path.abspath(__file__))
project_root = os.path.dirname(os.path.dirname(script_dir))
images_dir = os.path.join(project_root, 'backend', 'images')
delete_list_file = os.path.join(script_dir, 'images_to_delete.txt')

print("=" * 80)
print("VERWIJDEREN VAN ONGEBRUIKTE IMAGES")
print("=" * 80)
print(f"Images directory: {images_dir}")
print(f"Delete list: {delete_list_file}")
print()

# Lees de lijst met te verwijderen bestanden
if not os.path.exists(delete_list_file):
    print(f"FOUT: {delete_list_file} niet gevonden!")
    print("Run eerst identify_unused_images.py om de lijst te genereren.")
    sys.exit(1)

with open(delete_list_file, 'r', encoding='utf-8') as f:
    files_to_delete = [line.strip() for line in f if line.strip()]

print(f"Gevonden {len(files_to_delete)} bestanden om te verwijderen")
print()
print("LET OP: Dit verwijdert permanent bestanden!")
print("Weet je zeker dat je wilt doorgaan?")
print()
print("Voor automatische uitvoering, gebruik: python delete_unused_images.py --yes")
print()

# Check voor --yes flag
auto_confirm = '--yes' in sys.argv

if not auto_confirm:
    print("Geannuleerd (gebruik --yes flag voor automatische uitvoering)")
    sys.exit(0)

print()
print("Verwijderen van images...")

deleted_count = 0
not_found_count = 0
error_count = 0

for filename in files_to_delete:
    filepath = os.path.join(images_dir, filename)
    
    if os.path.exists(filepath):
        try:
            os.remove(filepath)
            deleted_count += 1
            if deleted_count % 100 == 0:
                print(f"  {deleted_count} images verwijderd...")
        except Exception as e:
            print(f"  FOUT bij verwijderen {filename}: {e}")
            error_count += 1
    else:
        not_found_count += 1

print()
print("=" * 80)
print("KLAAR!")
print("=" * 80)
print(f"Verwijderd: {deleted_count}")
print(f"Niet gevonden: {not_found_count}")
print(f"Fouten: {error_count}")
print()

