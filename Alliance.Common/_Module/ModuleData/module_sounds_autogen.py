import os
import re
import xml.etree.ElementTree as ET
from collections import defaultdict

# This script generates the module_sounds.xml file automatically based on sound files arborescence in ModuleSounds dir
def generate_xml(directory):
    base = ET.Element('base', attrib={
        'xmlns:xsi': "http://www.w3.org/2001/XMLSchema-instance",
        'xmlns:xsd': "http://www.w3.org/2001/XMLSchema",
        'type': "module_sound"
    })
    sounds = ET.SubElement(base, 'module_sounds')

    last_dir = None
    sound_files = defaultdict(list)

    for root, dirs, files in os.walk(directory):
        for file in files:
            if file.endswith(('.wav', '.mp3')):
                full_path = os.path.join(root, file)
                relative_path = os.path.relpath(full_path, directory).replace('\\', '/')
                base_name = re.sub(r'_[0-9]+$', '', os.path.splitext(file)[0])
                sound_files[base_name].append(relative_path)
    
    for base_name, files in sound_files.items():
        current_dir = os.path.dirname(files[0])
        if last_dir is None or last_dir != current_dir:
            comment_text = f"Sounds in {current_dir.replace('--', '-')}"
            sounds.append(ET.Comment(comment_text))
            last_dir = current_dir

        if len(files) > 1:
            event_name = "event:/al/" + current_dir.lower().replace(' ', '_') + '/' + base_name.lower().replace(' ', '_') + 's'
            sound_category = determine_sound_category(files[0])
            
            sound_element = ET.SubElement(sounds, 'module_sound', attrib={
                'name': event_name,
                'sound_category': sound_category,
                'min_pitch_multiplier': '0.9',
                'max_pitch_multiplier': '1.1'
            })

            for file in files:
                ET.SubElement(sound_element, 'variation', attrib={
                    'path': file,
                    'weight': '1'
                })
        else:
            event_name = "event:/al/" + files[0].lower().replace(' ', '_').replace('.wav', '').replace('.mp3', '')
            sound_category = determine_sound_category(files[0])
            
            sound_element = ET.SubElement(sounds, 'module_sound', attrib={
                'name': event_name,
                'sound_category': sound_category,
                'path': files[0]
            })
            if "2d" in files[0].lower() or "ambient" in files[0].lower():
                sound_element.set('is_2d', 'true')

    # Convert the created XML structure to a string and write to file
    tree = ET.ElementTree(base)
    ET.indent(tree, space="    ", level=0)
    tree.write('module_sounds.xml', encoding='utf-8', xml_declaration=True)

def determine_sound_category(relative_path):
    if "alert" in relative_path.lower():
        return "alert"
    elif "voice" in relative_path.lower():
        return "mission_voice"
    else:
        return "mission_ambient_3d_medium"

if __name__ == '__main__':
    generate_xml('../ModuleSounds')  # Change this path if running from a different directory
