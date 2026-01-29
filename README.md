# 🤖 GenAI Chatbot - RAG-Powered Q&A

A **Retrieval-Augmented Generation (RAG)** chatbot built with .NET 8.0, powered by OpenAI's GPT-5-Mini and Pinecone vector database for intelligent landmark queries.

---

## 🏗️ System Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    Frontend Layer                            │
│  (chat.html, question.html, searchchunks.html)              │
└────────────────┬────────────────────────────────────────────┘
                 │ HTTP (localhost:3000 CORS)
                 ▼
┌─────────────────────────────────────────────────────────────┐
│              ASP.NET Core Web API                           │
│  /search → Returns top 3 article chunks                    │
│  /ask    → Returns AI-generated answer                     │
└────┬──────────────┬──────────────────┬─────────────────────┘
     │              │                  │
     ▼              ▼                  ▼
┌──────────┐  ┌────────────┐  ┌────────────────┐
│ Vector   │  │ RAG        │  │ Prompt Service │
│ Search   │  │ Question   │  │ (System        │
│ Service  │  │ Service    │  │  Prompts)      │
└────┬─────┘  └────────────┘  └────────────────┘
     │              │
     ▼              ▼
┌──────────────────────────────────────────────────────────────┐
│           External Services & Storage                        │
│                                                              │
│  Pinecone      → Vector database (landmark-chunks index)   │
│  OpenAI API    → Embeddings + GPT-5-Mini                  │
│  SQLite        → Local article content                     │
│  Wikipedia     → Source data (via WikipediaClient)         │
└──────────────────────────────────────────────────────────────┘
```

---

## 📊 Data Flow: From Wikipedia to Q&A

```
Step 1: DATA SOURCE
    Wikipedia Articles (Landmarks)
              ↓
Step 2: PROCESSING
    ArticleSplitter.cs
    └─ Break into chunks (~300 words per chunk)
              ↓
Step 3: VECTORIZATION
    IndexBuilder.cs
    └─ Generate 512-dimensional embeddings
    └─ Upload to Pinecone
    └─ Store chunks in SQLite
              ↓
Step 4: READY FOR QUERIES
    ┌─────────────────────────────┐
    │ Pinecone: "landmark-chunks" │
    │ [512-dim vectors ready]     │
    │ Enables semantic search     │
    └─────────────────────────────┘
```

---

## 🔄 API Endpoints

### `/search` - Semantic Article Search
```
GET /search?query=ancient+monuments

Returns: Top 3 article chunks matching the query
{
  "id": "pyramid-chunk-5",
  "title": "Great Pyramid of Giza",
  "section": "Ancient History",
  "content": "Built approximately 4,500 years ago...",
  "sourcePageUrl": "https://wikipedia.org/wiki/Great_Pyramid"
}
```

### `/ask` - RAG Question Answering
```
GET /ask?question=How+tall+is+the+Eiffel+Tower?

Returns: AI-generated answer with sources
{
  "answer": "The Eiffel Tower is 330 meters (1,083 feet) tall...",
  "sources": ["https://wikipedia.org/wiki/Eiffel_Tower"]
}
```

---

##  Quick Start

### Prerequisites
- .NET 8.0 SDK
- `OPENAI_API_KEY` environment variable
- `PINECONE_API_KEY` environment variable

### Setup & Run
```bash
# Restore packages
dotnet restore

# Build
dotnet build

# Run API (localhost:5000)
dotnet run

# Build vector index (optional - uncomment in Program.cs)
# var indexer = app.Services.GetRequiredService<IndexBuilder>();
# await indexer.BuilderIndex(SourceData.LandmarkNames);
```

---

<div align="center">

**Built with ❤️ using .NET, AI, and Vector Databases**

</div>
