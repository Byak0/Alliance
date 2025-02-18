using BehaviorTrees.Nodes;
using System.Threading;
using System.Threading.Tasks;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Common.Extensions.AdvancedCombat.BTTasks
{
	public class LogTask : BTTask
	{
		private readonly string message;
		private readonly LogLevel logLevel;

		public LogTask(string message, LogLevel logLevel) : base()
		{
			this.message = message;
			this.logLevel = logLevel;
		}

		public override async Task<bool> Execute(CancellationToken cancellationToken)
		{
			Log(message, logLevel);
			return true;
		}
	}
}
