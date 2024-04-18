import { useState, useEffect, useRef } from "react";
import axios from "axios";
import "../css/Master.css";
import UpdateDetails from "./UpdateDetails";
import { CSVLink } from "react-csv";
import URL from "../data/url.json";

const UserDetails = () => {
  const [users, setUsers] = useState([]);
  const [seen, setSeen] = useState(false);
  const [user, setUser] = useState({});
  const [updatedOn, setUpdatedOn] = useState(0);
  const csvLink = useRef();

  useEffect(() => {
    axios
      .get(URL.url + "appinfo/GetLastHitByUserDetails")
      .then((res) => {
        console.log(res.data);
        setUsers(res.data);
      })
      .catch((err) => console.log(err));
  }, [updatedOn]);

  function togglePop(user) {
    if (user) {
      setUser(user);
    } else {
      setUpdatedOn(new Date());
    }
    setSeen(!seen);
  }

  const handleDelete = (userId) => {
    if (window.confirm("Are you sure to delete the user detail?")) {
      const payload = {
        user: userId,
      };
      axios
        .post(URL.url + "appinfo/DeleteLastHitDetail", payload, {
          headers: {
            "Content-Type": "application/json",
          },
        })
        .then((res) => {
          setUpdatedOn(new Date());
        });
    }
  };

  const download = () => {
    csvLink.current.link.click();
  };

  return (
    <>
      <div id="search">
        <button className="btn btn-info btn-sm" onClick={download}>
          Download to csv
        </button>
        <CSVLink
          data={users}
          filename="user.csv"
          className="hidden"
          ref={csvLink}
          target="_blank"
        />
      </div>
      <div className="table-responsive">
        <table className="table">
          <thead className="sticky-top bg-success p-2 text-white">
            <tr className="red">
              <th>#</th>
              <th>Desktop ID</th>
              <th>Last Hit On</th>
              <th>Name</th>
              <th>City</th>
              <th>Mobile No</th>
              <th>Address</th>
              <th className="text-break" style={{ maxWidth: "300px" }}>
                Allowed User Id(s)
              </th>
              <th>App Version</th>
              <th>Summary</th>
              <th>Actions</th>
            </tr>
          </thead>
          <tbody>
            {users.map((item, index) => {
              return (
                <tr
                  key={index}
                  style={{
                    backgroundColor: !item.inactive ? "" : "gray",
                    color: !item.inactive ? "" : "#fff",
                  }}
                >
                  <td>{index + 1} </td>
                  <td>{item.user} </td>
                  <td>{item.date}</td>
                  <td>{item.name}</td>
                  <td>{item.city}</td>
                  <td>{item.mobileNo} </td>
                  <td>{item.address} </td>
                  <td className="text-wrap"  style={{ maxWidth: "300px" }}>{item.allowedUserId}</td>
                  <td>{item.appVersion} </td>
                  <td>{item.summary} </td>
                  <td>
                    <div className="input-group mx-1">
                      <button
                        className="btn btn-primary btn-sm me-1"
                        onClick={() => togglePop(item)}
                      >
                        Update
                      </button>
                      <button
                        className="btn btn-danger btn-sm"
                        onClick={() => handleDelete(item.user)}
                      >
                        Delete
                      </button>
                    </div>
                  </td>
                </tr>
              );
            })}
          </tbody>
        </table>
      </div>
      {seen ? <UpdateDetails toggle={togglePop} user={user} /> : null}
    </>
  );
};

export default UserDetails;
