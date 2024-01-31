using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zeepkist.ResetTime
{
    internal static class VotingManager
    {
        static IList<ulong> voted = new List<ulong>();

        public static event EventHandler<NewVoteArgs> VoteChanges;

        public static Boolean NewVote(ulong playerId)
        {
            if (voted.Contains(playerId)){
                return false;
            }

            voted.Add(playerId);
            VoteChanges?.Invoke(null, new NewVoteArgs(voted.Count));
            return true;
        }

        public static Boolean RemoveVote(ulong playerId) {
            if (!voted.Contains(playerId))
            {
                return false;
            }

            voted.Remove(playerId);
            VoteChanges?.Invoke(null, new NewVoteArgs(voted.Count));
            return true;
        }

        public static void Clear()
        {
            voted.Clear();
        }
    }

    internal class NewVoteArgs : EventArgs
    {
        public int votes { get; set; }

        public NewVoteArgs(int votes)
        {
            this.votes = votes;
        }
    }
}
