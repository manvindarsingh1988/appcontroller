import { useState, useEffect, useRef }  from 'react';
import axios from 'axios';
import './Master.css';
import UpdateDetails from "./UpdateDetails";
import { CSVLink } from 'react-csv'

const UserDetails = () => {
    const [users, setUsers] = useState([]);
    const [seen, setSeen] = useState(false);
    const [user, setUser] = useState({});
    const [updatedOn, setUpdatedOn] = useState(0)
    const csvLink = useRef();

    useEffect(() => {
        axios.get('https://manvindarsingh.bsite.net/appinfo/GetLastHitByUserDetails')
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
            axios.post("https://manvindarsingh.bsite.net/appinfo/DeleteLastHitDetail", payload, {
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

       return (
        <div style={{margin: "20px"}}>
           <div>
           <button style={{marginBottom: "20px"}} onClick={download}>Download to csv</button>
           <CSVLink
         data={users}
         filename='user.csv'
         className='hidden'
         ref={csvLink}
         target='_blank'
      />
            </div> 
            <table>
                <thead>
                    <tr>
                        <th>#</th>
                        <th>Desktop ID</th>
                        <th>Last Hit On</th>
                        <th>Name</th>
                        <th>City</th>
                        <th>Mobile No</th>
                        <th>Address</th>
                        <th>Allowed User Id(s)</th>
                    </tr>
                </thead>
                <tbody>
                    {users.map((item, index) => {
                        return (
                        <tr key={index}
                        style={{
                            backgroundColor: !item.inactive ? "" : "red",
                            color: !item.inactive ? "" : '#fff'
                           }}
                        >
                            <td>{index + 1}</td>
                            <td>{item.user}</td>
                            <td>{item.date}</td>
                            <td>{item.name}</td>
                            <td>{item.city}</td>
                            <td>{item.mobileNo}</td>
                            <td>{item.address}</td>
                            <td>{item.allowedUserId}</td>
                            <td>
                                <button onClick={() => togglePop(item)}>Update</button>
                                <button style={{marginLeft: '5px'}} onClick={() => handleDelete(item.user)}>Delete</button>
                            </td>
                        </tr>
                        );
                    })}
                </tbody>
            </table> 
            {seen ? <UpdateDetails toggle={togglePop} user={user} /> : null}         
        </div>
      );
    };
    
    export default UserDetails;