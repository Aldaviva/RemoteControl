console.log("Starting common.js");

class AbstractSiteHandler {

	static jumpDurationMs = 5000;

	start() {
		chrome.runtime.onMessage.addListener(this.#onMessageFromServiceWorker);
	}

	#onMessageFromServiceWorker(request, sender, sendResponse) {
		console.debug("Received message from service worker", request, sender);

		let response = {
			exception: null
		};

		switch (request.name) {
			case "FetchPlaybackState":
				response.playbackState = this.fetchPlaybackState();
				break;
			case "PressButton":
				this.pressButton(request.button);
				response.website = this.websiteName;
				break;
			default:
				response.exception = "UnsupportedCommand";
				response.name = request.name;
				break;
		}

		sendResponse(response);
	}

	fetchPlaybackState() {
		console.error("fetchPlaybackState unimplemented");
	}

	pressButton(button) {
		console.error("pressButton unimplemented");
	}

	get websiteName() {
		return null;
	}

}