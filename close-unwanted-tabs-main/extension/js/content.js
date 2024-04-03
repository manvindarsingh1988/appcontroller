const BASE_URL = "https://manvindarsingh.bsite.net";

chrome.runtime.onMessage.addListener(async (message, sender, sendResponse) => {   
  var apiData = await getValidUrl(message.data.User)
  const urls = apiData?.urLs;
  if (message.action === "updateData") {
    const data = message.data;

    const isValid = urls.some((u) => message.urlToCheck.toLowerCase().includes(u.url.toLowerCase()));
    if (!isValid) {
      alert("Warning!! Restricted site...");
      await updateData(data);  
    }

    chrome.runtime.sendMessage({ action: "isValidUrl", data: isValid });
  }

  if(message.action === 'validateId') {  
    if(apiData?.ids) {
      let ele1 = document.getElementsByName('login'); 
      ele1[0].disabled = true;
      let ele = document.getElementsByName('csclogin');
      ele[0].addEventListener('blur', () => this.validateId(apiData?.ids, message.data.User), true);
    }
  }
});

async function validateId(ids, user){
  let inputFields = document.getElementsByName('csclogin');
  let x = inputFields[0].value;
  if(ids) {
    let list = ids.split(',');
    const isValid = list.some((u) => u.toLowerCase() === x.toLowerCase());
    if (!isValid) {
      let ele1 = document.getElementsByName('login'); 
      ele1[0].disabled = true;
      const userDetails = {
        Id: '',
        Date: '',
        AppName: 'connect.csc.gov.in',
        Summary: 'Tried to open by User Id: ' + x,
        User: user
      };
      await updateData(userDetails);
      alert("Warning!! Use aligned id only");
      return;
    }
  }
  var ele1 = document.getElementsByName('login'); 
  ele1[0].disabled = false;
}

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