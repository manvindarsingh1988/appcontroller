const BASE_URL = "https://manvindarsingh.bsite.net";

chrome.runtime.onMessage.addListener(async (message, sender, sendResponse) => {
  if (message.action === "updateData") {
    const urls = (await getValidUrl())?.urLs;
    const data = message.data;

    const isValid = urls.some((u) => message.urlToCheck.includes(u));
    if (!isValid) {
      alert("Warning!! Restricted site...");
      await updateData(data);
    }

    chrome.runtime.sendMessage({ action: "isValidUrl", data: isValid });
  }
});

async function updateData(data) {
  const apiUrl = `${BASE_URL}/appinfo`;

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
  } catch (error) {}
}

async function getValidUrl(user) {
  const d = await fetch(`${BASE_URL}/appinfo/GetValidURLs?user=${user}`);
  return await await d.json();
}
