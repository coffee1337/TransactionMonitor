using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;

namespace TransactionMonitor.Services
{
    public class CsvExportService
    {
        public async Task ExportAsync(List<string> headers, List<List<string>> rows, string filename, nint windowHandle)
        {
            var sb = new StringBuilder();

            sb.AppendLine(string.Join(";", headers));

            foreach (var row in rows)
            {
                sb.AppendLine(string.Join(";", row.Select(v => $"\"{v}\"")));
            }

            var picker = new FileSavePicker();
            WinRT.Interop.InitializeWithWindow.Initialize(picker, windowHandle);
            picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            picker.FileTypeChoices.Add("CSV файл", new List<string> { ".csv" });
            picker.SuggestedFileName = filename;

            var file = await picker.PickSaveFileAsync();
            if (file != null)
            {
                await FileIO.WriteTextAsync(file, sb.ToString(),
                    Windows.Storage.Streams.UnicodeEncoding.Utf8);
            }
        }
    }
}