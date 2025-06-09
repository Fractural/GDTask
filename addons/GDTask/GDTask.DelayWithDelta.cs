using System;
using System.Threading;


namespace Fractural.Tasks;

// ReSharper disable once InconsistentNaming
public readonly partial struct GDTask
{

	/// <summary>
	/// Similar as GDTask.Yield but guaranteed run on next frame.
	/// <br/>
	/// Runs in <see cref="PlayerLoopTiming"/>.<see cref="PlayerLoopTiming.Process"/>
	/// </summary>
	/// <param name="cancellationToken"> Optional cancellation token </param>
	/// <returns>Delta time between frames</returns>
	public static GDTask<double> NextFrameWithDelta(CancellationToken cancellationToken = default)
		=> GDTask.NextFrameWithDelta(PlayerLoopTiming.Process, cancellationToken);
	
	
	/// <summary>
	/// Similar as GDTask.Yield but guaranteed run on next frame.
	/// </summary>
	/// <param name="timing"> <see cref="PlayerLoopTiming"/> of frames </param>
	/// <param name="cancellationToken"> Optional cancellation token </param>
	/// <returns> Delta time between frames </returns>
	public static async GDTask<double> NextFrameWithDelta(PlayerLoopTiming timing, CancellationToken cancellationToken = default)
	{
		var beforeTime = DateTime.Now;
		
		await GDTask.NextFrame(timing, cancellationToken);

		return (DateTime.Now - beforeTime).TotalSeconds;
	}
	
	
	/// <summary>
	/// Waits <see cref="delayFrameCount"/> frames
	/// </summary>
	/// <param name="delayFrameCount">Amount of frames to wait</param>
	/// <param name="delayTiming"><see cref="PlayerLoopTiming"/> of frames</param>
	/// <param name="cancellationToken">Optional cancellation token</param>
	/// <returns>Delta time of delay. Expect it to be greater then 1 if <see cref="delayFrameCount"/> greater then FPS</returns>
	/// <exception cref="ArgumentOutOfRangeException"><see cref="delayFrameCount"/> less than 0 </exception>
	public static async GDTask<double> DelayFrameWithDelta(int delayFrameCount, PlayerLoopTiming delayTiming = PlayerLoopTiming.Process, CancellationToken cancellationToken = default)
	{
		if (delayFrameCount < 0)
			throw new ArgumentOutOfRangeException("Delay does not allow minus delayFrameCount. delayFrameCount:" + delayFrameCount);

		
		var beforeTime = DateTime.Now;
		
		await GDTask.DelayFrame(delayFrameCount, delayTiming, cancellationToken);
		
		return (DateTime.Now - beforeTime).TotalSeconds;
	}
}
