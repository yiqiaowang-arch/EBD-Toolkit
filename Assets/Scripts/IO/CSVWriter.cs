using System.Collections.Generic;
using System.IO;

public class CSVWriter
{
    /// <summary>
    /// Writes a CSV file with the given column names and data.
    /// </summary>
    /// <param name="filePath">Path to write the file to.</param>
    /// <param name="columnNames">Names for each column.</param>
    /// <param name="data">
    /// List of lists corresponding to values for each row. Number of items in a row (length of inner lists) needs to
    /// match the number of names for columns (length of <paramref name="columnNames">
    /// </param>
    /// <param name="separator">The seperator used to delimit items in a row. Defaults to `;`.</param>
    /// <exception cref="System.Exception">
    /// Throws and exception if length of <paramref name="columnNames"/> does not match length of each element of
    /// <paramref name="data"/>
    /// </exception>
    public static void WriteToCSV(
        string filePath,
        List<string> columnNames,
        List<List<string>> data,
        string separator = ";"
    )
    {
        // Check that the number of column names matches the number of columns in the data.
        foreach (List<string> row in data)
        {
            if (row.Count != columnNames.Count)
            {
                throw new System.Exception("Number of column names does not match number of columns in data.");
            }
        }

        using (StreamWriter writer = new StreamWriter(filePath))
        {
            // Write the column names.
            writer.WriteLine(string.Join(separator, columnNames.ToArray()));

            // Write the data.
            foreach (List<string> line in data)
            {
                writer.WriteLine(string.Join(separator, line.ToArray()));
            }
        }
    }
}