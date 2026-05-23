# Resume Parsing Implementation Report

## 1. Overview
The SmartJobPortal resume parsing pipeline processes uploaded PDF/DOCX resumes and extracts structured candidate data (personal details, skills, experience, education) using a **hybrid extraction approach**:
1. **Hybrid Extraction Service** – `HuggingFaceParserService` first attempts to call a Hugging Face model (currently bypassed) and then falls back to a **Gemini 1.5 Flash** request (`RescueWithGeminiV1`).
2. **Local Text Extraction** – `UploadResumeCommandHandler` extracts raw text from the file (PDF via `PdfPig`, DOCX via OpenXML) and sanitises it.
3. **Rule‑Based Extraction** – `ResumeSkillExtractor` runs regex‑based heuristics for email, skills, and years of experience.
4. **AI‑Enhanced Enrichment** – The Gemini payload further populates fields such as LinkedIn, GitHub, LeetCode, education, and work experience.
5. **Data Merge & Persistence** – Results are merged with existing candidate records, skills are de‑duplicated, and related entities are up‑serted.

## 2. Data Flow Diagram
```mermaid
flowchart TD
    A[UploadResumeCommand] --> B[ResumeFileValidator]
    B --> C[ExtractTextAsync]
    C --> D[ResumeTextSanitiser]
    D --> E[ResumeSkillExtractor (regex)]
    D --> F[HuggingFaceParserService (Gemini fallback)]
    E --> G[MergeSkills]
    F --> H[MergeEducation]
    F --> I[MergeExperience]
    G --> J[CandidateRepository]
    H --> J
    I --> J
    J --> K[Cache Invalidation]
    K --> L[API Response]
```

## 3. Key Components
| Component | Responsibility | Notable Code |
|-----------|----------------|--------------|
| `UploadResumeCommandHandler` | Orchestrates validation, text extraction, sanitisation, AI calls, merging, caching. | Lines 44‑168 (handler) |
| `ResumeFileValidator` | Checks file size ≤5 MB and allowed extensions (`.pdf`, `.docx`). | Lines 7‑24 |
| `ResumeSkillExtractor` | Regex‑based skill & experience extraction. | Lines 7‑30 |
| `HuggingFaceParserService` | Calls Gemini API (hard‑coded API key) to obtain structured JSON. | `RescueWithGeminiV1` (lines 37‑85) |
| `ResumeDto` & related DTOs | Target schema for API response and DB merging. | `ResumeDto.cs` (lines 5‑15) |

## 4. Security & Compliance Findings
1. **Hard‑coded Gemini API Key** – The key (`AIzaSyCDofbZ7T5vmA_dJniHNBjmPVOdSNKi75U`) is embedded in source code (see `HuggingFaceParserService.cs` line 39). This violates secret‑management best practices and can lead to credential leakage.
2. **Lack of Configuration‑Based Secrets** – The service already reads a Hugging Face API key from `IConfiguration`, but the Gemini key bypasses this mechanism.
3. **No Rate‑Limiting/Retry Logic** – The Gemini HTTP call has a single attempt; failures are swallowed after a generic catch, which could lead to silent data loss.
4. **Potential Injection in Prompt** – The raw resume text is interpolated directly into the Gemini prompt (`{text}`) without sanitisation, opening a possibility for prompt injection attacks.
5. **File System Paths** – Uploaded files are stored under a configurable `ResumeStorage:Path` but default is a relative path (`uploads/resumes`). Ensure the path is outside the web root to prevent direct URL access.

## 5. Data Mapping & Validation Gaps
- **Education Mapping** – `MergeEducationAsync` maps `e.Year` to `GraduationYear` without validating date format; invalid strings could break UI expectations.
- **Experience Duration** – The `Duration` field from AI is stored verbatim; no parsing to a numeric value for further calculations.
- **Skills Normalisation** – Skills are lower‑cased for de‑duplication, but UI may display mixed case. Consider persisting a canonical version and a display label.
- **Email Extraction** – Regex may capture malformed emails; additional validation (e.g., `MailAddress` parsing) is advisable.

## 6. Recommendations
1. **Move Gemini API Key to Configuration**
   ```csharp
   var geminiKey = _config["Gemini:ApiKey"] ?? string.Empty;
   ```
   Update `appsettings.json` and use secret‑management (Azure Key Vault, User‑Secrets) for production.
2. **Sanitise Prompt Input**
   - Escape newline characters and JSON‑special characters.
   - Optionally strip any "`#`" or "`{{`" sequences that could affect the model.
3. **Add Retry & Circuit‑Breaker**
   - Use `Polly` policies for transient failures when calling Gemini.
4. **Validate AI‑Returned JSON**
   - Schematize the expected JSON (e.g., using `System.Text.Json` with `JsonSerializerOptions` and custom converters) and log validation errors.
5. **Improve Experience Parsing**
   - Extract start/end dates and compute total months/years for more accurate seniority scoring.
6. **Enhance Skill Matching**
   - Integrate a synonym dictionary (e.g., map "JS" → "JavaScript") and use fuzzy matching for partial skill names.
7. **Secure File Storage**
   - Store resumes in a directory outside the web‑server's static file path; serve them via a secured endpoint that checks user permissions.
8. **Unit Tests**
   - Add tests for `ResumeFileValidator`, `ResumeSkillExtractor`, and the `ParseJson` method to ensure resilience against malformed inputs.
9. **Logging & Telemetry**
   - Emit structured logs (e.g., using Serilog) for each stage, including duration and success/failure metrics.
10. **Documentation Update**
    - Document the full flow in the project wiki, highlighting the security considerations and configuration requirements.

## 7. Conclusion
The current resume parsing implementation is functional and demonstrates a robust hybrid AI‑plus‑regex approach. However, critical security and reliability gaps (hard‑coded API key, lack of input sanitisation, minimal error handling) need to be addressed before moving to production. Implementing the recommendations above will improve maintainability, security, and the overall quality of candidate data extraction.
