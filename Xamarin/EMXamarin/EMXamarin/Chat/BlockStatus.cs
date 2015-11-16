using System;

namespace em
{
	public enum BlockStatus
	{
		NotBlocked,
		HasBlockedUs,
		Blocked,
		BothBlocked
	}

	public static class BlockStatusHelper {
		public static BlockStatus FromDatabase(string s) {
			switch (s) {
			case "B":
				return BlockStatus.Blocked;

			default:
			case "N":
				return BlockStatus.NotBlocked;

			case "H":
				return BlockStatus.HasBlockedUs;

			case "2":
				return BlockStatus.BothBlocked;
			}
		}

		public static string ToDatabase(BlockStatus status) {
			switch (status) {
			default:
			case BlockStatus.Blocked:
				return "B";

			case BlockStatus.NotBlocked:
				return "N";

			case BlockStatus.HasBlockedUs:
				return "H";

			case BlockStatus.BothBlocked:
				return "2";
			}
		}

		public static BlockStatus FromString(string s) {
			switch (s) {
			case "Blocked":
				return BlockStatus.Blocked;

			default:
			case "NotBlocked":
				return BlockStatus.NotBlocked;

			case "HasBlockedUs":
				return BlockStatus.HasBlockedUs;

			case "BothBlocked":
				return BlockStatus.BothBlocked;
			}
		}

		public static bool CanSend(BlockStatus bs) {
			switch(bs) {
			default:
			case BlockStatus.NotBlocked:
				return true;

			case BlockStatus.Blocked:
			case BlockStatus.HasBlockedUs:
			case BlockStatus.BothBlocked:
				return false;
			}
		}
	}
}

