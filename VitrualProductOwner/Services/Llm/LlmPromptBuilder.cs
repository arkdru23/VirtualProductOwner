using System.Text;
using System.Text.Json;

namespace BlazorApp1.Services.Llm;

public static class LlmPromptBuilder
{
    public static string Build(string context)
    {
        var sb = new StringBuilder();
        sb.AppendLine("You are an experienced Product Owner assistant. Analyze the provided context and generate comprehensive user stories.");
        sb.AppendLine();
        sb.AppendLine("Context:");
        sb.AppendLine(context ?? string.Empty);
        sb.AppendLine();
        sb.AppendLine("Task:");
        sb.AppendLine("Generate detailed user stories in JSON format with the following schema:");
        sb.AppendLine("{");
        sb.AppendLine("  \"stories\": [");
        sb.AppendLine("    {");
        sb.AppendLine("      \"title\": \"Short descriptive title\",");
        sb.AppendLine("      \"description\": \"Detailed description in 'As a [user], I want [feature] so that [benefit]' format\",");
        sb.AppendLine("      \"points\": 5,");
        sb.AppendLine("      \"acceptanceCriteria\": \"Given...; When...; Then...\",");
        sb.AppendLine("      \"area\": \"Feature area (e.g., Authentication, Reporting, Dashboard)\",");
        sb.AppendLine("      \"iteration\": \"Sprint/Iteration name if mentioned\",");
        sb.AppendLine("      \"state\": \"New\",");
        sb.AppendLine("      \"priority\": 2,");
        sb.AppendLine("      \"risk\": \"Risk level: Low, Medium, or High\",");
        sb.AppendLine("      \"useCase\": \"Primary use case or scenario\"");
        sb.AppendLine("    }");
        sb.AppendLine("  ]");
        sb.AppendLine("}");
        sb.AppendLine();
        sb.AppendLine("Rules:");
        sb.AppendLine("- points: Must be between 1-13 (Fibonacci: 1,2,3,5,8,13). Base on complexity and effort.");
        sb.AppendLine("- title: Concise, clear, under 80 characters");
        sb.AppendLine("- description: Follow 'As a ... I want ... so that ...' pattern when possible");
        sb.AppendLine("- acceptanceCriteria: Use Given-When-Then format or bullet points separated by ';'");
        sb.AppendLine("- area: Identify the feature area or domain (e.g., 'User Management', 'Reporting', 'Security')");
        sb.AppendLine("- state: Always 'New' for generated stories");
        sb.AppendLine("- priority: 1 (Highest) to 4 (Lowest). Base on business value and urgency.");
        sb.AppendLine("- risk: Assess technical or business risk: 'Low', 'Medium', or 'High'");
        sb.AppendLine("- useCase: Main scenario or use case (optional, omit if not clear)");
        sb.AppendLine("- iteration: Sprint/iteration name only if explicitly mentioned in context");
        sb.AppendLine();
        sb.AppendLine("Example:");
        sb.AppendLine("{");
        sb.AppendLine("  \"stories\": [");
        sb.AppendLine("    {");
        sb.AppendLine("      \"title\": \"User Password Reset via Email\",");
        sb.AppendLine("      \"description\": \"As a user, I want to reset my password via email so that I can regain access to my account if I forget my password\",");
        sb.AppendLine("      \"points\": 5,");
        sb.AppendLine("      \"acceptanceCriteria\": \"Given user forgot password; When user clicks 'Forgot Password'; Then user receives reset email; When user clicks reset link; Then user can set new password; Then user can login with new password\",");
        sb.AppendLine("      \"area\": \"Authentication\",");
        sb.AppendLine("      \"state\": \"New\",");
        sb.AppendLine("      \"priority\": 2,");
        sb.AppendLine("      \"risk\": \"Medium\",");
        sb.AppendLine("      \"useCase\": \"Password recovery\"");
        sb.AppendLine("    }");
        sb.AppendLine("  ]");
        sb.AppendLine("}");
        return sb.ToString();
    }

    public static string BuildRefine(string extraContext, string currentStoryJson)
    {
        var sb = new StringBuilder();
        sb.AppendLine("You are a Product Owner assistant. Refine the provided user story using the extra context and feedback.");
        sb.AppendLine();
        sb.AppendLine("Extra context / Feedback:");
        sb.AppendLine(extraContext ?? string.Empty);
        sb.AppendLine();
        sb.AppendLine("Current story JSON:");
        sb.AppendLine(currentStoryJson);
        sb.AppendLine();
        sb.AppendLine("Task: Improve the story based on the feedback. Return a single JSON object with fields:");
        sb.AppendLine("{");
        sb.AppendLine("  \"title\": \"Improved title\",");
        sb.AppendLine("  \"description\": \"Improved description\",");
        sb.AppendLine("  \"points\": 5,");
        sb.AppendLine("  \"acceptanceCriteria\": \"Improved criteria\",");
        sb.AppendLine("  \"area\": \"Feature area\",");
        sb.AppendLine("  \"priority\": 2,");
        sb.AppendLine("  \"risk\": \"Low/Medium/High\",");
        sb.AppendLine("  \"useCase\": \"Primary use case\"");
        sb.AppendLine("}");
        sb.AppendLine();
        sb.AppendLine("Rules:");
        sb.AppendLine("- points: 1-13 only (Fibonacci)");
        sb.AppendLine("- title: Concise and descriptive");
        sb.AppendLine("- description: 'As a ... I want ... so that ...' when possible");
        sb.AppendLine("- acceptanceCriteria: Given-When-Then format or bullet points separated by ';'");
        sb.AppendLine("- priority: 1 (High) to 4 (Low)");
        sb.AppendLine("- risk: Low, Medium, or High");
        return sb.ToString();
    }

    public static string ToJson(object obj) => JsonSerializer.Serialize(obj);
}
