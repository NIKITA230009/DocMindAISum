using DocMind.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace DocMind.Core.Interfaces
{
    public interface IDocumentService
    {
        Task<DocumentModel> LoadDocumentAsync(string filePath);
        Task SaveDocumentAsync(string filePath, DocumentModel document);
    }
}
