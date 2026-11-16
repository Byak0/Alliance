using System;
using System.Collections.Generic;

namespace Alliance.Common.GameModes.Story.Validation
{
	public enum ValidationSeverity { Error, Warning, Info }

	[Serializable]
	public class ValidationIssue
	{
		public ValidationSeverity Severity { get; set; }
		public string Path { get; set; }
		public string Message { get; set; }

		public ValidationIssue() { }

		public ValidationIssue(ValidationSeverity severity, string path, string message)
		{
			Severity = severity;
			Path = path;
			Message = message;
		}

		public string SeverityIcon => Severity switch
		{
			ValidationSeverity.Error => "[ERROR]",
			ValidationSeverity.Warning => "[WARN] ",
			ValidationSeverity.Info => "[INFO] ",
			_ => "       "
		};

		public override string ToString() => $"{SeverityIcon} {Path}: {Message}";
	}
}
