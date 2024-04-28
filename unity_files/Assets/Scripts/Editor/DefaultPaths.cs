using System.IO;

namespace EBD
{
    public static class DefaultPaths
    {
        public static string RawDataPath = Path.Combine("Data", "VirtualWalkthrough", "Raw");
        public static string ProcessedDataPath = Path.Combine("Data", "VirtualWalkthrough", "Processed");
        public static string FinalDataPath = Path.Combine("Data", "VirtualWalkthrough", "Final");
    }
}