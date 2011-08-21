from xml.dom import minidom

#will be removed from all monobehaviours
members_to_remove = set([".ctor", "Start", "Update", "Awake", "OnApplicationQuit",
                     "FixedUpdate", "LateUpdate", "OnGUI"])

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
        if member.getAttribute("MemberName") in members_to_remove:
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

def main():
    
    import sys
    from os import path, makedirs
    import glob
    print 'Warning: script does not preserve directory trees'    
    # parse command-line
    if (len(sys.argv) != 2):
        print 'usage: %s <xml file or directory with xmls>' % sys.argv[0]
        print '       will output into working directory/out'
        print '       directory scanning is NOT recursive'
        return
    file = sys.argv[1]
    out_dir = 'out'
    if (path.isfile(file)):
        file_list = [file]
    else:
        file_list = glob.glob(path.join(file, "*.xml"))
    # prepare output
    try:
        makedirs("out") # hard-code output dir for now
    except OSError:
        print 'Failed to create output dir. Already exists?'
    # do stuff 
    for file_path in file_list:
        print 'handling file: %s' % file_path
        new_data = fix_xml_file(file_path)
        open(path.join(out_dir, path.split(file_path)[1]), "w").write(new_data)

if __name__ == '__main__':
    main()