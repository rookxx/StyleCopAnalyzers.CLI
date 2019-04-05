namespace StyleCopAnalyzers.CLI
{
    using System;
    using System.Collections.Immutable;
    using System.IO;
    using System.Threading.Tasks;
    using Microsoft.CodeAnalysis;

    public class FixedDocumentContextWriter : IFixedContextWriter
    {
        void IFixedContextWriter.Write(FixedDocumentContext context)
        {
            WriteChangedDocuments(context.FixedResult.ChangedDocuments);

            var directoryPath = Path.GetDirectoryName(context.Document.FilePath);
            AddNewDocuments(directoryPath, context.FixedResult.AddedDocuments);

            RemoveDocuments(context.FixedResult.RemovedDocuments);
        }

        private void WriteChangedDocuments(ImmutableArray<Document> documents)
        {
            foreach(var document in documents)
            {
                var path = document.FilePath;
                var text = document.GetTextAsync().Result;
                Console.WriteLine("        Changed:" + path);
                File.WriteAllText(path, text.ToString());
            }
        }

        private void AddNewDocuments(string directoryPath, ImmutableArray<Document> documents)
        {
            foreach(var document in documents)
            {
                var fileName = document.Name;
                var path = Path.Combine(directoryPath, fileName);
                var text = document.GetTextAsync().Result;
                Console.WriteLine("        Added:" + path);
                File.WriteAllText(path, text.ToString());
            }
        }

        private void RemoveDocuments(ImmutableArray<Document> documents)
        {
            foreach(var document in documents)
            {
                Console.WriteLine("        Removed:" + document.FilePath);
                File.Delete(document.FilePath);
            }
        }
    }
}