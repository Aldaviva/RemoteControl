console.log("Starting twitch.js");

class TwitchHandler extends AbstractSiteHandler {

	get websiteName() {
		return "TWITCH";
	}

	get playButton() {
		return document.querySelectorAll(".video-player__default-player button[data-a-target='player-play-pause-button']");
	}

	get canSeek() {
		return document.querySelectorAll(".video-player__default-player div[data-a-target='player-seekbar']") !== null;
	}

	fetchPlaybackState() {
		const playButton = this.playButton;
		return {
			isPlaying: playButton.dataset.aPlayerState === "playing",
			canPlay: !!playButton
		};
	}

	pressButton(button) {
		const playButton = this.playButton;
		if (playButton) {
			switch (button) {
				case "PLAY_PAUSE":
					playButton.click();
					break;
				case "PREVIOUS_TRACK":
					if(this.canSeek){
						document.activeElement?.blur();
						document.dispatchEvent(new KeyboardEvent("keydown", { keyCode: 37 })); // ArrowLeft
					}
					break;
				case "NEXT_TRACK":
					if(this.canSeek){
						document.activeElement?.blur();
						document.dispatchEvent(new KeyboardEvent("keydown", { keyCode: 39 })); // ArrowRight
					}
					break;
				case "STOP":
					if (playButton.dataset.aPlayerState === "playing") {
						playButton.click();
					}
					break;
				case "MEMORY":
					this.blurPage();
					// server will then send an "F" keystroke to Vivaldi, putting the video in fullscreen, since it requires an authentic mouse or keyboard input, and not a synthetic click like our extension can create
					break;
				default:
					break;
			}
		}
	}

}

new TwitchHandler().start();