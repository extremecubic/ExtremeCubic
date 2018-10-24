using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using MEC;
using System;

public class StartCounterUI : MonoBehaviour
{
	[SerializeField] Text _counterText;
	CoroutineHandle _handle;
	Action _onDone;

	public void StartCount(double delta, Double time, Action action)
	{
		Timing.KillCoroutines(_handle);

		_onDone = action;
		_handle = Timing.RunCoroutine(_countDown(delta, time));
	}

	public void CancelCount()
	{
		_counterText.gameObject.SetActive(false);
		Timing.KillCoroutines(_handle);
		_onDone = null;
	}
	
	IEnumerator<float> _countDown(double delta, double time)
	{
		double timer = time;

		// remove the net delta if in online game
		if (Constants.onlineGame)
		    timer = time - (PhotonNetwork.time - delta);

		_counterText.gameObject.SetActive(true);

		while(timer > 0)
		{
			timer -= Time.deltaTime;
			if (_counterText == null)
				yield break;

			_counterText.text = timer.ToString("0");

			yield return Timing.WaitForOneFrame;
		}

		_counterText.gameObject.SetActive(false);

		// inovke the delegate if on have been bessed in
		_onDone?.Invoke();

	}
}
