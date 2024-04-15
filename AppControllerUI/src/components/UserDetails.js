import { useState, useEffect, useRef }  from 'react';
import axios from 'axios';
import '../css/Master.css';
import UpdateDetails from "./UpdateDetails";
import { CSVLink } from 'react-csv'
import URL from '../data/url.json';

const UserDetails = () => {
    const [users, setUsers] = useState([]);
    const [seen, setSeen] = useState(false);
    const [user, setUser] = useState({});
    const [updatedOn, setUpdatedOn] = useState(0)
    const csvLink = useRef();

    useEffect(() => {
        axios.get(URL.url + 'appinfo/GetLastHitByUserDetails')
        .then(res => {
            console.log(res.data);
            setUsers(res.data);
        }) 
        .catch(err => console.log(err));
       }, [updatedOn]);

    function togglePop (user) {
        if(user){
            setUser(user);
        } else {
            setUpdatedOn(new Date());
        }     
        setSeen(!seen);
    };

    const handleDelete = (userId) => { 
        if(window.confirm('Are you sure to delete the user detail?')) {
          const payload = {
              user: userId
            }        
            axios.post(URL.url + "appinfo/DeleteLastHitDetail", payload, {
              headers: {
                'Content-Type': 'application/json'
              }
            })
            .then(res => {
              setUpdatedOn(new Date());
          });
        }
      };

      const download = () => {
        csvLink.current.link.click()
      }
      let windowWidth = window.innerWidth;
      windowWidth = windowWidth - 1400;
       return (
        <>
           <div id="search">
           <button onClick={download}>Download to csv</button>
           <CSVLink
                data={users}
                filename='user.csv'
                className='hidden'
                ref={csvLink}
                target='_blank'
            />
            </div> 
            <div id="tablediv">
                <div id="tablediv-container" style={{width: "calc(100% - 18px)", marginLeft: "8px"}}>  
                    <table className="userscrolldown">
                        <thead>
                            <tr className='red'>
                                <th><div style={{width: "30px"}}>#</div></th>
                                <th><div style={{width: "150px"}}>Desktop ID</div></th>
                                <th><div style={{width: "150px"}}>Last Hit On</div></th>
                                <th><div style={{width: "150px"}}>Name</div></th>
                                <th><div style={{width: "150px"}}>City</div></th>
                                <th><div style={{width: "100px"}}>Mobile No</div></th>
                                <th><div style={{width: "150px"}}>Address</div></th>
                                <th><div style={{width: windowWidth}}>Allowed User Id(s)</div></th>
                                <th><div style={{width: "150px"}}>App Version</div></th>
                                <th><div style={{width: "150px"}}>Summary</div></th>
                                <th><div style={{width: "150px"}}>Actions</div></th>
                            </tr>
                        </thead>
                        <tbody>
                            {users.map((item, index) => {
                                return (
                                <tr key={index}
                                style={{
                                    backgroundColor: !item.inactive ? "" : "gray",
                                    color: !item.inactive ? "" : '#fff'
                                }}
                                >
                                    <td><div style={{width: "30px"}}>{index + 1}</div></td>
                                    <td><div style={{width: "150px"}}>{item.user}</div></td>
                                    <td><div style={{width: "150px"}}>{item.date}</div></td>
                                    <td><div style={{width: "150px"}}>{item.name}</div></td>
                                    <td><div style={{width: "150px"}}>{item.city}</div></td>
                                    <td><div style={{width: "100px"}}>{item.mobileNo}</div></td>
                                    <td><div style={{width: "150px"}}>{item.address}</div></td>
                                    <td><div style={{width: windowWidth, wordWrap: 'break-word'}}>{item.allowedUserId}</div></td>
                                    <td><div style={{width: "150px"}}>{item.appVersion}</div></td>
                                    <td><div style={{width: "150px"}}>{item.summary}</div></td>
                                    <td>
                                        <div style={{width: "150px"}}>
                                            <button onClick={() => togglePop(item)}>Update</button>
                                            <button style={{marginLeft: '5px'}} onClick={() => handleDelete(item.user)}>Delete</button>
                                        </div>
                                    </td>
                                </tr>
                                );
                            })}
                        </tbody>
                    </table> 
                </div>
            </div>
            {seen ? <UpdateDetails toggle={togglePop} user={user} /> : null}         
        </>
      );
    };
    
    export default UserDetails;