chrome.runtime.onMessage.addListener(async (message, sender, sendResponse) => {
    if (message.action === "updateData") {
        const urls = await getValidUrl();
        const data = message.data;

        const isValid = urls.some(
            (u) =>
                message.urlToCheck.includes(u.url) &&
                (u.user.trim() === "" || data.User == u.user)
        );
        if (!isValid) {
            alert("Warning!! Restricted site...");
            await updateData(data);
        }

        chrome.runtime.sendMessage({ action: "isValidUrl", data: isValid });
    }
});

async function updateData(data) {
    const apiUrl = "https://www.appcontroller.in/appinfo";

    const userDetails = data;

    try {
        const response = await fetch(apiUrl, {
            method: "POST",
            headers: {
                "Content-Type": "application/json",
            },
            body: JSON.stringify(userDetails),
        });

        if (!response.ok) {
            throw new Error(`API request failed with status: ${response.status}`);
        }

        await response.json();
    } catch (error) { }
}

async function getValidUrl() {
    const d = await fetch(
        "https://www.appcontroller.in/appinfo/GetValidURLs"
    );
    return await await d.json();
}
