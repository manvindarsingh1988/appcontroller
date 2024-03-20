import { useState }  from 'react';
import axios from 'axios';
import './UpdateDetails.css';

const UpdateDetails = (props) => {
    const [name, setName] = useState(props.user.name);
    const [city, setCity] = useState(props.user.city);
    const [mobileNo, setMobileNo] = useState(props.user.mobileNo);
    const [address, setAddress] = useState(props.user.address);
    function handleLogin(e) {
        e.preventDefault()
        const payload = {
            id: props.user.id,
            name: name,
            city: city,
            mobileNo: mobileNo,
            address: address,
            user: props.user.user
          } 
          axios.post('https://www.appcontroller.in/appinfo/UpdateUserDetail', payload)
          .then(response => {
            alert('User detail added.')
            props.toggle()
          })
          .catch(error => {
            if (error.response) {
              console.log(error.response.data);
              console.log(error.response.status);
              console.log(error.response.headers);
            } else if (error.request) {
              console.log(error.request);
            } else {
              console.log('Error', error.message);
            }
            console.log(error.config);
          });
        
    }

    return (
        <div className="popup">
            
            <div className="popup-inner">  
                <button className="close" onClick={props.toggle}></button>    
                <div className='popup-inner-div'>
                    <h2>{props.user.user}</h2>
                    <form onSubmit={handleLogin}>
                        <label>
                            Name:
                            <input type="text" value={name} onChange={e => setName(e.target.value)} />
                        </label>
                        <label>
                            City:
                            <input type="text" value={city} onChange={e => setCity(e.target.value)} />
                        </label>
                        <label>
                            Mobile No:
                            <input type="text" value={mobileNo} onChange={e => setMobileNo(e.target.value)} />
                        </label>
                        <label>
                            Address:
                            <input type="text" value={address} onChange={e => setAddress(e.target.value)} />
                        </label>
                        <button type="submit">Update</button>
                    </form>
                </div>                   
            </div>
            
        </div>
    )
};

export default UpdateDetails;