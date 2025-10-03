using System;

namespace GodDance.Source
{
	public class PreloadOperation
	{
		public PreloadOperation.PreloadState State
		{
			get
			{
				object obj = this.stateLock;
				PreloadOperation.PreloadState result;
				lock (obj)
				{
					result = this.state;
				}
				return result;
			}
			private set
			{
				object obj = this.stateLock;
				lock (obj)
				{
					this.state = value;
				}
			}
		}
		public bool IsEmpty { get; private set; } = true;
		public SaveStats SaveStats { get; private set; }
		public string Message { get; private set; }
		public PreloadOperation(int saveSlot, GameManager gm)
		{
			this.SaveSlot = saveSlot;
			this.gm = gm;
			this.GetSaveStatsForSlot();
		}
		private void GetSaveStatsForSlot()
		{
			bool flag = this.State > PreloadOperation.PreloadState.NotStarted;
			if (!flag)
			{
				this.State = PreloadOperation.PreloadState.Loading;
				this.gm.HasSaveFile(this.SaveSlot, delegate(bool inUse)
				{
					bool flag2 = !this.killed;
					if (flag2)
					{
						bool flag3 = !inUse;
						if (flag3)
						{
							this.IsEmpty = true;
							this.SetComplete();
						}
						else
						{
							this.IsEmpty = false;
							this.gm.GetSaveStatsForSlot(this.SaveSlot, delegate(SaveStats stats, string message)
							{
								bool flag4 = !this.killed;
								if (flag4)
								{
									this.Message = message;
									bool flag5 = stats != null;
									if (flag5)
									{
										this.SaveStats = stats;
									}
									this.SetComplete();
								}
							});
						}
					}
				});
			}
		}

		// Token: 0x06000010 RID: 16 RVA: 0x00002288 File Offset: 0x00000488
		public void WaitForComplete(Action<PreloadOperation.PreloadState> onComplete)
		{
			bool flag = this.killed;
			if (flag)
			{
				bool flag2 = onComplete != null;
				if (flag2)
				{
					onComplete(PreloadOperation.PreloadState.Complete);
				}
			}
			else
			{
				object obj = this.stateLock;
				lock (obj)
				{
					bool flag4 = this.state != PreloadOperation.PreloadState.Complete;
					if (flag4)
					{
						this.callback = (Action<PreloadOperation.PreloadState>)Delegate.Combine(this.callback, onComplete);
						return;
					}
				}
				bool flag5 = onComplete != null;
				if (flag5)
				{
					onComplete(this.State);
				}
			}
		}

		// Token: 0x06000011 RID: 17 RVA: 0x0000232C File Offset: 0x0000052C
		private void SetComplete()
		{
			bool flag = this.killed;
			if (!flag)
			{
				object obj = this.stateLock;
				lock (obj)
				{
					this.state = PreloadOperation.PreloadState.Complete;
				}
				bool flag3 = this.callback != null;
				if (flag3)
				{
					CoreLoop.InvokeSafe(delegate()
					{
						this.callback(PreloadOperation.PreloadState.Complete);
					});
				}
			}
		}
		public void Kill()
		{
			this.killed = true;
		}
		public readonly int SaveSlot;
		private PreloadOperation.PreloadState state;
		private object stateLock = new object();
		private GameManager gm;
		private Action<PreloadOperation.PreloadState> callback;
		private bool killed;
		public enum PreloadState
		{
			NotStarted,
			Loading,
			Complete
		}
	}
}
