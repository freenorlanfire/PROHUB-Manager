using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using PROHUB.API.Server.Dtos;

namespace PROHUB.API.Server.Services;

/// <summary>
/// Integrates with Anthropic Claude API to enhance project context docs and provide AI analysis.
/// Configure via Anthropic:ApiKey in appsettings / environment variable.
/// Falls back gracefully if no key is present.
/// </summary>
public class AiService(HttpClient http, IConfiguration config, ILogger<AiService> logger)
{
    private const string API_URL  = "https://api.anthropic.com/v1/messages";
    private const string MODEL    = "claude-3-5-haiku-20241022";
    private const int    MAX_TOKENS = 4096;

    private string? ApiKey => config["Anthropic:ApiKey"];

    public bool IsConfigured => !string.IsNullOrWhiteSpace(ApiKey);

    // ── Enhance / generate context doc ─────────────────────────────────────────

    public async Task<AiEnhanceResponse> EnhanceContextDocAsync(
        string projectName,
        string? projectDescription,
        string? projectStatus,
        string[] tags,
        string[] integrationTypes,
        string existingDoc,
        string? extraInstructions,
        CancellationToken ct = default)
    {
        if (!IsConfigured)
            return new AiEnhanceResponse(
                GenerateFallbackDoc(projectName, projectDescription, projectStatus, tags),
                "fallback",
                false);

        var systemPrompt = """
            You are PROHUB AI — an expert senior software architect and project manager.
            Your job is to write and enhance project context documentation in clean Markdown.
            Rules:
            - Write concise, developer-friendly documentation
            - Use Markdown headers, bullet points, and code fences
            - Focus on architecture, tech stack, integrations, and current status
            - Do NOT add placeholder text like [INSERT X] — only write what you know
            - Keep it under 1500 words
            """;

        var tagSection = tags.Length > 0
            ? $"\nTechnologies/Tags: {string.Join(", ", tags)}"
            : "";

        var intSection = integrationTypes.Length > 0
            ? $"\nKnown integrations: {string.Join(", ", integrationTypes)}"
            : "";

        var existingSection = string.IsNullOrWhiteSpace(existingDoc)
            ? "\nNo existing context doc — generate one from scratch."
            : $"\n\nExisting context doc (enhance/improve this):\n```\n{existingDoc}\n```";

        var extraSection = string.IsNullOrWhiteSpace(extraInstructions)
            ? ""
            : $"\n\nExtra instructions from the user: {extraInstructions}";

        var userMessage = $"""
            Project: {projectName}
            Description: {projectDescription ?? "(none)"}
            Current status: {projectStatus ?? "unknown"}{tagSection}{intSection}{existingSection}{extraSection}

            Write a complete, professional context doc for this project. Include:
            1. Overview (what the project does, why it exists)
            2. Tech Stack (based on tags/integrations)
            3. Architecture overview
            4. Current status and active work
            5. Key links / integrations (if known)
            6. Notes for developers joining the project
            """;

        try
        {
            var response = await CallClaudeAsync(systemPrompt, userMessage, ct);
            return new AiEnhanceResponse(response, MODEL, true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[AI] EnhanceContextDoc failed for project {Name}", projectName);
            return new AiEnhanceResponse(
                GenerateFallbackDoc(projectName, projectDescription, projectStatus, tags),
                "fallback-error",
                false);
        }
    }

    // ── AI project analysis / tips ──────────────────────────────────────────────

    public async Task<string> AnalyzeProjectAsync(
        string projectName,
        string? description,
        string status,
        string[] tags,
        string contextDoc,
        List<string> recentStatusNotes,
        CancellationToken ct = default)
    {
        if (!IsConfigured)
            return "⚠️ AI analysis requires an Anthropic API key. Add `Anthropic__ApiKey` to your environment variables.";

        var systemPrompt = """
            You are PROHUB AI — an expert tech project advisor.
            Analyze the project data and provide 3-5 concrete, actionable insights.
            Format: bullet points. Be specific, not generic.
            Tone: direct, professional, brief.
            """;

        var recentNotes = recentStatusNotes.Count > 0
            ? $"\nRecent status notes:\n{string.Join("\n", recentStatusNotes.Select(n => $"- {n}"))}"
            : "";

        var docSection = !string.IsNullOrWhiteSpace(contextDoc)
            ? $"\n\nContext doc:\n{contextDoc[..Math.Min(contextDoc.Length, 1000)]}"
            : "";

        var userMessage = $"""
            Project: {projectName}
            Status: {status}
            Tags: {string.Join(", ", tags)}{recentNotes}{docSection}

            Provide 3-5 actionable insights about this project. Focus on: risks, quick wins, process improvements, or tech debt.
            """;

        try
        {
            return await CallClaudeAsync(systemPrompt, userMessage, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[AI] AnalyzeProject failed for {Name}", projectName);
            return "Failed to generate AI analysis. Please try again later.";
        }
    }

    // ── Core Claude API call ────────────────────────────────────────────────────

    private async Task<string> CallClaudeAsync(string systemPrompt, string userMessage, CancellationToken ct)
    {
        var body = new
        {
            model = MODEL,
            max_tokens = MAX_TOKENS,
            system = systemPrompt,
            messages = new[]
            {
                new { role = "user", content = userMessage }
            }
        };

        var json = JsonSerializer.Serialize(body);
        var request = new HttpRequestMessage(HttpMethod.Post, API_URL)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        request.Headers.Add("x-api-key", ApiKey);
        request.Headers.Add("anthropic-version", "2023-06-01");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var res = await http.SendAsync(request, ct);

        if (!res.IsSuccessStatusCode)
        {
            var errBody = await res.Content.ReadAsStringAsync(ct);
            logger.LogError("[AI] Claude API error {Status}: {Body}", res.StatusCode, errBody);
            throw new InvalidOperationException($"Claude API returned {res.StatusCode}");
        }

        using var stream = await res.Content.ReadAsStreamAsync(ct);
        var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

        // Response: { "content": [{ "type": "text", "text": "..." }] }
        var text = doc.RootElement
            .GetProperty("content")[0]
            .GetProperty("text")
            .GetString();

        return text ?? throw new InvalidOperationException("Empty response from Claude");
    }

    // ── Fallback (no API key) ───────────────────────────────────────────────────

    private static string GenerateFallbackDoc(
        string name, string? description, string? status, string[] tags)
    {
        var tagLine = tags.Length > 0 ? $"\n- **Stack:** {string.Join(", ", tags)}" : "";
        return $"""
            # {name}

            ## Overview
            {description ?? "Add a description for this project."}

            ## Status
            Current status: **{status ?? "active"}**

            ## Tech Stack{tagLine}

            ## Architecture
            _Describe the architecture here._

            ## Getting Started
            _Add setup instructions here._

            ## Notes
            _Add any relevant notes for the team._

            ---
            *Generated by PROHUB. Add your Anthropic API key to enable AI-enhanced docs.*
            """;
    }
}
