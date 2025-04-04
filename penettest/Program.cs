using PeNet;
using PeNet.Header.Pe;
// See https://aka.ms/new-console-template for more information

ReadFile(args[0]);

void ReadFile(string filePath)
{
    if(PeFile.TryParse(filePath, out var peFile))
    {
        ArgumentNullException.ThrowIfNull(peFile);
        ImageSectionHeader? resourceSectionHeader = null;
        if (peFile.ImageSectionHeaders != null)
        {
            foreach (var section in peFile.ImageSectionHeaders)
            {
                Console.WriteLine($"name = {section.Name}, sizeofraw = {section.SizeOfRawData}, vsize = {section.VirtualSize}, ptraw = {section.PointerToRawData}({section.PointerToRawData:x}), vaddr = {section.VirtualAddress:x}, char = {section.Characteristics}");
                Console.WriteLine($"  {string.Join("|", section.CharacteristicsResolved)}");
                if (section.Name.Equals(".rsrc", StringComparison.OrdinalIgnoreCase))
                {
                    resourceSectionHeader = section;
                }
            }
        }
        ArgumentNullException.ThrowIfNull(resourceSectionHeader);
        if (peFile.ImageResourceDirectory != null)
        {
            OutputResource(peFile, resourceSectionHeader, peFile.ImageResourceDirectory, 0);
        }
        
    }
}

void OutputResource(PeFile peFile, ImageSectionHeader resourceSectionHeader, ImageResourceDirectory imageResourceDirectory, int depth)
{
    var indent = new string(' ', depth * 4);
    Console.WriteLine($"{indent}major = {imageResourceDirectory.MajorVersion}, minor = {imageResourceDirectory.MinorVersion},char = {imageResourceDirectory.Characteristics:x}, idnum = {imageResourceDirectory.NumberOfIdEntries}");
    if(imageResourceDirectory.DirectoryEntries == null)
    {
        return;
    }
    foreach (var item in imageResourceDirectory.DirectoryEntries)
    {
        if (item == null)
        {
            Console.WriteLine($"item is null");
            continue;
        }
        Console.WriteLine($"{indent}name = {item.Name},resolved = {item.NameResolved}, id = {item.ID}, offset = {item.OffsetToData}({item.OffsetToData:x}), isdir = {item.DataIsDirectory}, isId = {item.IsIdEntry}, isNamed = {item.IsNamedEntry}");
        if (item.ResourceDataEntry != null)
        {
            Console.WriteLine($"{indent}dataentry: offset = {item.ResourceDataEntry.OffsetToData}({item.ResourceDataEntry.OffsetToData:x}),cp = {item.ResourceDataEntry.CodePage},size1 = {item.ResourceDataEntry.Size1}");
            var dataOffsetOfFile = item.ResourceDataEntry.OffsetToData - resourceSectionHeader.VirtualAddress + resourceSectionHeader.PointerToRawData;
            var span = peFile.RawFile.AsSpan(dataOffsetOfFile, item.ResourceDataEntry.Size1);
            Console.WriteLine($"{indent}{Convert.ToBase64String(span)}");
        }
        if (item.ResourceDirectory != null)
        {
            OutputResource(peFile, resourceSectionHeader, item.ResourceDirectory, depth + 1);
        }
    }
}

