import axiosClient from './axiosClient';

/**
 * Cập nhật trạng thái của nhân viên (Probation, Active, OnLeave, Terminated)
 * Endpoint: PATCH /api/employees/{employeeId}/status
 */
export async function changeEmployeeStatusApi(employeeId, status) {
  const res = await axiosClient.patch(`/Employees/${employeeId}/status`, JSON.stringify(status), {
    headers: {
      'Content-Type': 'application/json',
    },
  });
  return res.data;
}

export default {
  changeEmployeeStatusApi,
};
