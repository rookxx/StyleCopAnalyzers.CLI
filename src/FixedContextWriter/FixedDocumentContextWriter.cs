namespace StyleCopAnalyzers.CLI
{
    using System;
    using System.Collections.Immutable;
    using System.IO;
    using System.Threading.Tasks;
    using Microsoft.CodeAnalysis;

    public class FixedDocumentContextWriter : IFixedContextWriter
    {
        private ILogger logger = new SilentLogger();

        void IFixedContextWriter.SetLogger(ILogger logger)
        {
            this.logger = logger;
        }

        void IFixedContextWriter.Write(FixedDocumentContext context)
        {
            this.logger.LogInformation("    Fix: " + context.Document.FilePath);
            WriteChangedDocuments(context.FixedResult.ChangedDocuments);

            var directoryPath = Path.GetDirectoryName(context.Document.FilePath);
            AddNewDocuments(directoryPath!, context.FixedResult.AddedDocuments);

            RemoveDocuments(context.FixedResult.RemovedDocuments);
        }

        private void WriteChangedDocuments(ImmutableArray<Document> documents)
        {
            foreach (var document in documents)
            {
                var path = document.FilePath;
                var text = document.GetTextAsync().Result;
                this.logger.LogVerbose("        Changed:" + path);
                File.WriteAllText(path, text.ToString());
            }
        }

        private void AddNewDocuments(string directoryPath, ImmutableArray<Document> documents)
        {
            foreach (var document in documents)
            {
                var fileName = document.Name;
                var path = Path.Combine(directoryPath, fileName);
                var text = document.GetTextAsync().Result;
                this.logger.LogVerbose("        Added:" + path);
                File.WriteAllText(path, text.ToString());
            }
        }

        private void RemoveDocuments(ImmutableArray<Document> documents)
        {
            foreach (var document in documents)
            {
                this.logger.LogVerbose("        Removed:" + document.FilePath);
                File.Delete(document.FilePath);
            }
        }
    }
}