from xml.dom import minidom

#will be removed from all monobehaviours
MEMBERS_TO_REMOVE = set([".ctor", "Start", "Update", "Awake", "OnApplicationQuit",
                     "FixedUpdate", "LateUpdate", "OnGUI"])

INDEX_FILE = "index.xml"
OUT_DIR = "out"

def remove_node(node):
    node.parentNode.removeChild(node)

def get_base_class(type_root):
    base_types = type_root.getElementsByTagName("BaseTypeName")
    if (len(base_types) != 1):
        print "shit man! %d base types" % len(base_types)
        return
    base_type = base_types[0]
    if not base_type.hasChildNodes():
        print "awww, no child nodes to BaseTypeName"
        return
    
    return base_type.childNodes[0].nodeValue

def remove_monobehavior_members(type_root):
    for member in type_root.getElementsByTagName("Member"):
        if member.getAttribute("MemberName") in MEMBERS_TO_REMOVE:
            print "removing member <%s>" % member.getAttribute("MemberName")
            remove_node(member)

def fix_type(type_root):
    print "fixing type '%s'" % type_root.getAttribute("Name")
    
    for node in type_root.getElementsByTagName("AssemblyInfo"):
        print "removing AssemblyInfo"
        remove_node(node)
        
    if (get_base_class(type_root) == "UnityEngine.MonoBehaviour"):
        remove_monobehavior_members(type_root)

def fix_xml_file(file_path):
    root = minidom.parse(file_path)
    types = root.getElementsByTagName("Type")
    for class_type in types:
        fix_type(class_type)
    return root.toxml()

def fix_index_file(src, dst, whitelist):
    root = minidom.parse(src)
    types = root.getElementsByTagName("Type")
    for type_node in types:
        if type_node.getAttribute("Name") not in whitelist:
            print 'Removing type %s from index' % type_node.getAttribute('Name')
            remove_node(type_node)
    with open(dst, "w") as f:
        f.write(root.toxml())

def main():
    
    import sys
    from os import path, makedirs
    import glob
    # parse command-line
    if (len(sys.argv) != 2):
        print 'usage: %s <directory with xmls>' % sys.argv[0]
        print '       will output into working directory/out'
        print '       directory scanning is NOT recursive'
        return
    file = sys.argv[1]
    out_dir = 'out'
    if (path.isfile(file)):
        print 'Eek - name is a file and not a directory!'
        sys.exit(1)
    else:
        file_list = glob.glob(path.join(file, "*.xml"))
        
    # prepare output
    try:
        makedirs(OUT_DIR) # hard-code output dir for now
    except OSError:
        print 'Faild to create output dir. Already exists?'

    whitelist = set(line.strip() for line in open("docs.txt") if not line.startswith(";"))

    # modify index.xml - special case
    print file_list
    print path.join(file, INDEX_FILE)
    file_list.remove(path.join(file, INDEX_FILE))

    fix_index_file(path.join(file, INDEX_FILE), path.join(OUT_DIR, INDEX_FILE), whitelist)

    # filter class files from file list
    whitelist_files = set(cls + ".xml" for cls in whitelist)
    file_list = set(f for f in file_list if path.split(f)[-1] in whitelist_files)

    # modify all other xmls
    for file_path in file_list:
        print 'handling file: %s' % file_path
        new_data = fix_xml_file(file_path)
        open(path.join(out_dir, path.split(file_path)[1]), "w").write(new_data)

if __name__ == '__main__':
    main()