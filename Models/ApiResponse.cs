﻿namespace AonFreelancing.Models
{
    public class ApiResponse<T>
    {
        public bool IsSuccess { get; set; }
        public T Results { get; set; }
        public IList<Error> Errors { get; set; }
    }



    public class Error
    {
        public string Code { get; set; }
        public string Message { get; set; }
    }

    public class ApiLoginResponse<T>
    {
        public bool IsSuccess { get; set; }
        public T Results { get; set; }

        public String AccessToken { get; set; }
        public IList<Error> Errors { get; set; }
    }

}
