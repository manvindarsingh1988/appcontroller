import { useState, useEffect }  from 'react';
import axios from 'axios';
import './Master.css';

const UserDetails = () => {
    const [users, setUsers] = useState([]);
    useEffect(() => {
        axios.get('https://manvindarsingh.bsite.net/appinfo/GetLastHitByUserDetails')
        .then(res => {
            console.log(res.data);
            setUsers(res.data);
        }) 
        .catch(err => console.log(err));
       }, []);

       return (
        <div style={{margin: "20px"}}>
            <table>
                <thead>
                    <tr>
                        <th>Name</th>
                        <th>Last Hit On</th>
                    </tr>
                </thead>
                <tbody>
                    {users.map((item, index) => {
                        return (
                        <tr key={index}>
                            <td>{item.user}</td>
                            <td>{item.date}</td>
                        </tr>
                        );
                    })}
                </tbody>
            </table>          
        </div>
      );
    };
    
    export default UserDetails;