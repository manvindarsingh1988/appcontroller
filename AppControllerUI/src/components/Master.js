import { useState, useEffect } from "react";
import axios from "axios";
import "../css/App.css";
import URL from "../data/url.json";

const Master = () => {
  const [updatedOn, setUpdatedOn] = useState(0);
  const [checked, setChecked] = useState(false);
  const [validity, setValidity] = useState(0);
  const [apps, setApps] = useState([]);
  const [appVersion, setAppVersion] = useState("");
  const [downloaderVersion, setDownloaderVersion] = useState("");
  const [formValues, setFormValues] = useState({
    name: "",
    type: "URL",
    user: "",
  });
  useEffect(() => {
    axios
      .get(URL.url + "appinfo/GetApplicationSettings")
      .then((res) => {
        console.log(res.data);
        setChecked(res.data.killApps);
        setApps(res.data.allowedAppsAndUrls);
        setValidity(res.data.userValidity);
        setAppVersion(res.data.appVersion);
        setDownloaderVersion(res.data.downloaderVersion);
      })
      .catch((err) => console.log(err));
  }, [updatedOn]);

  const handleChange = (e) => {
    if (
      window.confirm(
        e.target.checked
          ? "Are you sure to change this flag, It will start killing apps on all linked systems?"
          : "Are you sure to change this flag, It will stop killing apps on all linked systems?"
      )
    ) {
      setChecked(e.target.checked);
      const payload = {
        killApp: e.target.checked,
      };

      axios
        .post(URL.url + "appinfo/AddKillAppSetting", payload, {
          headers: {
            "Content-Type": "application/json",
          },
        })
        .then((res) => {});
    }
  };

  const handleDelete = (id) => {
    if (
      window.confirm(
        "Are you sure to delete this app or URL, It will start killing this app or URL on all linked systems?"
      )
    ) {
      const payload = {
        id: id,
      };
      axios
        .post(URL.url + "appinfo/DeleteURLOrApp", payload, {
          headers: {
            "Content-Type": "application/json",
          },
        })
        .then((res) => {
          setUpdatedOn(new Date());
        });
    }
  };

  const handleNameAndType = (e) => {
    const { name, value } = e.target;
    setFormValues({ ...formValues, [name]: value });
  };

  const handleSave = (e) => {
    if (formValues.name) {
      const payload = {
        name: formValues.name,
        type: formValues.type,
        user: formValues.user,
      };

      axios
        .post(URL.url + "appinfo/AddURLOrApp", payload, {
          headers: {
            "Content-Type": "application/json",
          },
        })
        .then((res) => {
          setUpdatedOn(new Date());
          setFormValues({
            name: "",
            type: "URL",
            user: "",
          });
        });
    }
  };

  const updateValidity = () => {
    if (validity) {
      const payload = {
        validity: validity,
      };

      axios
        .post(URL.url + "appinfo/UpdateValidity", payload, {
          headers: {
            "Content-Type": "application/json",
          },
        })
        .then((res) => {
          alert("Active time updated successfully.");
          setUpdatedOn(new Date());
        });
    }
  };

  const st = {
    width: "auto",
    border: "1px solid black",
    padding: "10px",
    marginBottom: "10px",
    marginRight: "20px",
  };

  return (
    <>
      <div id="search">
        <label>
          <input
            className="m-2"
            type="checkbox"
            checked={checked}
            onChange={handleChange}
          />
          Kill Apps
        </label>
        <div className="border border-1 radius-2 m-2 p-2">
          <label className="form-label">
            User Active Check Time in Minutes:
            <input
              className="form-control"
              name="user"
              type="text"
              value={validity}
              onChange={(e) => setValidity(e.target.value)}
            />
          </label>
          <button className="btn btn-primary ms-2" onClick={updateValidity}>
            Update
          </button>
        </div>
        <div className="border border-1 radius-2 m-2 p-2">
          <label className="form-label">
            Latest Application Version:
            <b>{appVersion}</b>
          </label>
        </div>
        <div className="border border-1 radius-2 m-2 p-2">
          <label className="form-label">
            Latest Downloader Version:
            <b>{downloaderVersion}</b>
          </label>
        </div>
        <p className="ms-2 mt-2 mb-0">
          <b>Allowed Apps and URLs:</b>
        </p>
        <div className="border border-1 radius-2 m-2 p-2">
          <label className="form-label  me-2">
            Enter Url or App Name:
            <input
              className="form-control"
              name="name"
              type="text"
              value={formValues.name}
              onChange={handleNameAndType}
            />
          </label>
          <label className="form-label me-2">
            Enter User:
            <input
              className="form-control"
              name="user"
              type="text"
              value={formValues.user}
              onChange={handleNameAndType}
            />
          </label>
          <label className="form-label me-2">
            Pick a Type:
            <select
              className="form-select"
              id="selectedtype"
              name="type"
              onChange={handleNameAndType}
              value={formValues.type}
            >
              <option value="URL">URL</option>
              <option value="App">App</option>
            </select>
          </label>
          <button className="btn btn-primary btn ms-2" onClick={handleSave}>
            Add
          </button>
        </div>
      </div>
      <div className="table-responsive" id="tablediv">
        <table className="table mx-3">
          <thead className="sticky-top bg-success p-2 text-white">
            <tr className="red">
              <th>Name</th>
              <th>User</th>
              <th>Type</th>
              <th>Actions</th>
            </tr>
          </thead>
          <tbody>
            {apps.map((item, index) => {
              return (
                <tr key={index}>
                  <td>{item.name}</td>
                  <td>{item.user}</td>
                  <td>{item.type}</td>
                  <td>
                    <button
                      className="btn btn-danger btn-sm"
                      onClick={() => handleDelete(item.id)}
                    >
                      Delete
                    </button>
                  </td>
                </tr>
              );
            })}
          </tbody>
        </table>
      </div>
    </>
  );
};

export default Master;
