/**
 * Copyright 2023 Google LLC
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     https://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

mergeInto(LibraryManager.library, {
	// Every SDK callback below delivers its result with unityGameInstance.SendMessage, but the page
	// only assigns that global once createUnityInstance() resolves, which is after the engine has
	// already started running C#. A loadData() answer or a pause fired in that window used to throw
	// on the undefined global and the engine never heard about it. This helper retries until the
	// instance exists instead of dropping the event. Local fix (2026-09-07), not upstream.
	$ytgameSend: function (method, arg) {
		var attempts = 0;
		var deliver = function () {
			var instance = window.unityGameInstance;
			if (instance) {
				if (arg === undefined) instance.SendMessage("YTGameWrapper", method);
				else instance.SendMessage("YTGameWrapper", method, arg);
				return;
			}
			if (++attempts > 600) {
				console.warn("ytgame: dropped " + method + ", unityGameInstance never appeared");
				return;
			}
			setTimeout(deliver, 50);
		};
		deliver();
	},
    GetYTGameSDKVersion: function () {
		var sdkVersion = ytgame.SDK_VERSION;
		var bufferSize = lengthBytesUTF8(sdkVersion) + 1;
		var strBuffer = _malloc(bufferSize);
		stringToUTF8(sdkVersion, strBuffer, bufferSize);
		return strBuffer;
    },
	SendYTGameScore: function (score) {
		ytgame.engagement.sendScore({value: score});
	},
	SendFirstFrameReady: function () {
		ytgame.game.firstFrameReady();
	},
	SendGameReady: function () {
		ytgame.game.gameReady();
	},
	SaveGameData: function (playableSaveData) {
		ytgame.game.saveData(UTF8ToString(playableSaveData)).then(() => {
			// Handle data save success.
		  	return 0;
		}, (e) => {
		  	// console.error(e);
	  		// Send an error to YouTube
		  	ytgame.health.logError();

		  	// Handle data save failure.
		  	return 1;
		});
	},
	LoadGameData__deps: ['$ytgameSend'],
	LoadGameData: function () {
		ytgame.game.loadData().then((data) => {
			ytgameSend("ReceiveOnLoadSaveEvent", data == null ? "" : data.toString());
		}, (e) => {
			// A rejected load must still answer the game, or it waits on the save forever.
			// An empty string is what a first-run player gets too, so the game starts fresh.
			ytgame.health.logError();
			ytgameSend("ReceiveOnLoadSaveEvent", "");
		});
	},
	SendGameError: function (errorMessage) {
		console.error(errorMessage);
		// Send an error to YouTube
		ytgame.health.logError();
	},
	SendGameWarning: function (warningMessage) {
		console.debug(warningMessage);
		// Send an warning to YouTube
		ytgame.health.logWarning();
	},
	CheckIsAudioEnabled: function () {
		var isEnabled = Boolean(ytgame.system.isAudioEnabled());
		return isEnabled;
	},
	SetCallbackOnAudioEnabledChange__deps: ['$ytgameSend'],
	SetCallbackOnAudioEnabledChange: function () {
		const unsetCallback = ytgame.system.onAudioEnabledChange((isAudioEnabled) => {
			ytgameSend("ReceiveOnAudioEnabledChange", isAudioEnabled.toString());
		 });
	},
	SetCallbackOnGamePause__deps: ['$ytgameSend'],
	SetCallbackOnGamePause: function () {
		const unsetCallback = ytgame.system.onPause(() => {
			ytgameSend("ReceiveOnPauseEvent");
		 });
	},
	SetCallbackOnGameResume__deps: ['$ytgameSend'],
	SetCallbackOnGameResume: function () {
		const unsetCallback = ytgame.system.onResume(() => {
			ytgameSend("ReceiveOnResumeEvent");
		 });
	},
	CheckInPlayablesEnv: function () {
		var isPlayables = Boolean(typeof ytgame !== 'undefined' && ytgame.IN_PLAYABLES_ENV);
		return isPlayables;
	},
	RequestAnInterstitialAd: function () {
		ytgame.ads.requestInterstitialAd().then(() => {
			// Ad request successful, pause and mute calls should also be sent to your game.
		  	return 0;
		}, (e) => {
		  	// Ad not shown as there is a delay or none are available. Other failures will be picked up by YT.
		  	return 1;
		});
	},
	RequestAnRewardedAd__deps: ['$ytgameSend'],
	RequestAnRewardedAd: async function (rewardId) {
		var id = UTF8ToString(rewardId);
		try {
			const isRewardEarned = await ytgame.ads.requestRewardedAd(id);
			ytgameSend("ReceiveOnRewardedAdEvent", Boolean(isRewardEarned).toString());
		} catch (error) {
			ytgameSend("ReceiveOnRewardedAdEvent", "false");
		}
	},
});
