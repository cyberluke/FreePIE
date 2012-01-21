using System.Collections.Generic;

namespace FreePIE.Core.Common
{
    public interface IFileSystem
    {
        IEnumerable<string> GetFiles(string path, string pattern);
    }
}