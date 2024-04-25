import os
import xml.etree.ElementTree as ET

def generate_xml(directory):
    base = ET.Element('base', attrib={
        'xmlns:xsi': "http://www.w3.org/2001/XMLSchema-instance",
        'xmlns:xsd': "http://www.w3.org/2001/XMLSchema",
        'type': "module_sound"
    })
    sounds = ET.SubElement(base, 'module_sounds')

    last_dir = None
    for root, dirs, files in os.walk(directory):
        for file in files:
            if file.endswith(('.wav', '.mp3')):
                full_path = os.path.join(root, file)
                relative_path = os.path.relpath(full_path, directory).replace('\\', '/')
                
                current_dir = os.path.dirname(relative_path)
                if last_dir is None or last_dir != current_dir:
                    comment_text = f"Sounds in {current_dir.replace('--', '-')}"
                    sounds.append(ET.Comment(comment_text))
                    last_dir = current_dir

                event_name = "event:/al/" + relative_path.lower().replace(' ', '_').replace('.wav', '').replace('.mp3', '')
                
                # Determine sound category from directory or filename, here simplified:
                if "alert" in relative_path.lower():
                    sound_category = "alert"
                elif "voice" in relative_path.lower():
                    sound_category = "mission_voice"
                else:
                    sound_category = "mission_ambient_3d_medium"

                sound_element = ET.SubElement(sounds, 'module_sound', attrib={
                    'name': event_name,
                    'sound_category': sound_category,
                    'path': relative_path
                })
                if "2d" in root.lower() or "ambient" in root.lower():
                    sound_element.set('is_2d', 'true')

    # Convert the created XML structure to a string and write to file
    tree = ET.ElementTree(base)
    ET.indent(tree, space="    ", level=0)
    tree.write('module_sounds.xml', encoding='utf-8', xml_declaration=True)

if __name__ == '__main__':
    generate_xml('../ModuleSounds')  # Change this path if running from a different directory
