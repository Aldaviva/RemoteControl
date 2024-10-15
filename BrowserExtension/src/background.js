console.log("Starting background.js");

chrome.runtime.onStartup.addListener(() => console.log("Browser started"));

let webSocket;
connect();

function connect() {
	webSocket = new WebSocket("ws://localhost:4772/ws");

	webSocket.onopen = event => {
		console.info("WebSocket opened");
	};

	webSocket.onmessage = async event => {
		console.debug("WebSocket received message", event);

		let request;
		let response = { exception: null };

		try {
			request = JSON.parse(event.data);
			console.debug(`Handling ${request.name} command from server`);
		} catch(e) {
			console.error("Error processing server request", e);
			return;
		}

		const isPressButtonRequest = request.name === "PressButton";
		const isChannelUpButtonRequest = isPressButtonRequest && request.button === "CHANNEL_UP";
		if (isPressButtonRequest && (isChannelUpButtonRequest || request.button === "CHANNEL_DOWN")) {
			console.info(`Pressing ${request.button} button`);
			await changeChannel(isChannelUpButtonRequest);
		} else {
			response = await sendMessageToActivePage(request);
		}

		response.requestId = request.requestId;
		webSocket.send(JSON.stringify(response));
		console.debug("Sent response to server", response);
	};

	webSocket.onclose = event => {
		webSocket = null;
		console.info("WebSocket closed, reconnecting...");
		setTimeout(connect, 1000);
	};
}

setInterval(() => webSocket?.send("\"ping\""), 20*1000); // extension is killed after 30 seconds of no WebSocket messages

async function sendMessageToActivePage(command) {
	const [activeTab] = await chrome.tabs.query({ active: true, lastFocusedWindow: true });
	// console.debug("Active tab:", activeTab);

	if (activeTab) {
		try {
			const response = await chrome.tabs.sendMessage(activeTab.id, command);
			if (response) {
				return response;
			} else {
				console.warn("Active tab responded to our message with", response);
			}
		} catch(e) {
			console.warn("Active tab threw", e);
		}
	}

	console.error("runtime.lastError = ", chrome.runtime.lastError);

	return {
		exception: "UnsupportedWebsite",
		url: activeTab?.url ?? null
	};
}

async function changeChannel(isUp) {
	const currentWindowTabs = (await chrome.windows.getCurrent({ populate: true })).tabs;
	const oldActiveTabIndex = currentWindowTabs.findIndex(tab => tab.active);
	const newActiveTabIndex = (oldActiveTabIndex + (isUp ? 1 : -1)) % currentWindowTabs.length;
	const newActiveTab = currentWindowTabs[newActiveTabIndex];
	await chrome.tabs.update(newActiveTab.id, { active: true });
}
