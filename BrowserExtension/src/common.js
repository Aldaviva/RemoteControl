console.log("Starting common.js");

class AbstractSiteHandler {

	static jumpDurationMs = 5000;

	start() {
		chrome.runtime.onMessage.addListener(this.#onMessageFromServiceWorker);
	}

	#onMessageFromServiceWorker(request, sender, sendResponse) {
		console.debug("Received message from service worker", request, sender);

		let response = {
			requestId: request.requestId,
			result: null
		};

		switch (request.name) {
			case "FetchPlaybackState":
				response.playbackState = this.fetchPlaybackState();
				break;
			case "PressButton":
				response.website = this.pressButton(request.button);
				break;
			default:
				console.warn(`Unknown command from server: ${request.name}`);
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

}