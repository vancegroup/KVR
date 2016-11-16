using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EigenWrapper;

namespace KinectWithVRServer
{
    //This abstract class implements the math of a generic Kalman filter
    //However, it doesn't define any of the system models, the derived class is required to do that
    internal abstract class KalmanFilter
    {
        protected Matrix F;  //State transition matrix (physics model)
        protected Matrix Q;  //Process noise covariance matrix
        protected Matrix H;  //Observation model (sensor model)
        protected Matrix R;  //Observation noise covariance

        protected Matrix XLastMeasured;
        protected Matrix PLastMeasured;
        protected double timeSeconds;

        protected virtual Matrix getFMatrix(double deltaT)
        {
            return null;
        }

        protected virtual Matrix getQMatrix(double deltaT)
        {
            return null;
        }

        protected virtual Matrix getHMatrix()
        {
            return null;
        }

        protected virtual Matrix getRMatrix()
        {
            return null;
        }

        public Matrix PredictAndDiscard(double deltaT)
        {
            Matrix predictedX;
            lock (XLastMeasured)
            {
                predictedX = getFMatrix(deltaT) * XLastMeasured;
            }
            return predictedX;
        }

        public Matrix PredictAndDiscard(double deltaT, out Matrix covariance)
        {
            Matrix predictedX;
            lock (XLastMeasured)
            {
                predictedX = getFMatrix(deltaT) * XLastMeasured;
                covariance = F * PLastMeasured * Matrix.Transpose(F) + getQMatrix(deltaT);  //Covariance estimate
            }
            return predictedX;
        }

        public Matrix IntegrateMeasurement(Matrix measurement, double deltaT)
        {
            //"Prediction" Step
            Matrix Xpredicted = getFMatrix(deltaT) * XLastMeasured;  //State estimate
            Matrix Ppredicted = F * PLastMeasured * Matrix.Transpose(F) + getQMatrix(deltaT);  //Covariance estimate

            //"Update" step
            Matrix Y = measurement - H * Xpredicted;  //Measurement residual
            Matrix S = H * Ppredicted * Matrix.Transpose(H) + R;  //Residual covariance
            Matrix K = Ppredicted * Matrix.Transpose(H) * S.Inverse();  //Kalman gain
            Matrix X = Xpredicted + K * Y; //Updated state
            Matrix P = (Matrix.Identity(K.Rows) - K * H) * Ppredicted;

            lock (XLastMeasured)
            {
                timeSeconds += deltaT;
                XLastMeasured = X;
                PLastMeasured = P;
            }

            return X;
        }
    }

    internal class JerkConst3DFilter : KalmanFilter
    {
        // This implementation of the Kalman filter is for a 3D point and assumes a constant jerk.
        // Also note, this Kalman filter ignores any measured points that occured before the latest measurement
        //
        // The state vector is in the following format:
        //     ┌   x   ┐
        //     |  xdot |
        //     |xdotdot|
        // x = |   y   |
        //     |  ydot |
        //     |ydotdot|
        //     |   z   |
        //     |  zdot |
        //     └zdotdot┘
        //
        //The measurement vector is in the following format:
        //     ┌x┐
        // z = |y|
        //     └z┘

        private double sigmaxSensor = 0.2;  //These are inital guesses that can be overridden by using the appropriate IntegrateMeasurement function
        private double sigmaySensor = 0.2;
        private double sigmazSensor = 0.2;
        private double sigmaxActual = 1;    //These are related to the physics of a human moving 
        private double sigmayActual = 1;
        private double sigmazActual = 1;
        private DateTime? lastTime = null;

        internal JerkConst3DFilter()
        {
            //Set the initial position to (0, 0, 0), with speed (0, 0, 0) and acceleration (0, 0, 0)
            XLastMeasured = new Matrix(9, 1);

            //Initialize the covariance really high along the diagonal (i.e., we don't know the initial conditions)
            PLastMeasured = Matrix.Identity(9) * 10000;

            //Initialize the constant matrices
            getHMatrix();
            getRMatrix();
        }

        protected override Matrix getFMatrix(double deltaT)
        {
            if (F == null)
            {
                F = Matrix.Identity(9);
            }

            double t2D2 = deltaT * deltaT / 2;
            F[0, 1] = deltaT;
            F[0, 2] = t2D2;
            F[1, 2] = deltaT;

            F[3, 4] = deltaT;
            F[3, 5] = t2D2;
            F[4, 5] = deltaT;

            F[6, 7] = deltaT;
            F[6, 8] = t2D2;
            F[7, 8] = deltaT;

            return F;
        }

        protected override Matrix getQMatrix(double deltaT)
        {
            if (Q == null)
            {

                Q = new Matrix(9, 9);
            }

            //Test it with the values derived in the book
            double t5D20 = Math.Pow(deltaT, 5) / 20.0;
            double t4D8 = Math.Pow(deltaT, 4) / 8.0;
            double t3D6 = Math.Pow(deltaT, 3) / 6.0;
            double t3D3 = Math.Pow(deltaT, 3) / 3.0;
            double t2D2 = Math.Pow(deltaT, 2) / 2.0;

            Q[0, 0] = t5D20  * sigmaxActual;
            Q[0, 1] = t4D8   * sigmaxActual;
            Q[0, 2] = t3D6   * sigmaxActual;
            Q[1, 0] = t4D8   * sigmaxActual;
            Q[1, 1] = t3D3   * sigmaxActual;
            Q[1, 2] = t2D2   * sigmaxActual;
            Q[2, 0] = t3D6   * sigmaxActual;
            Q[2, 1] = t2D2   * sigmaxActual;
            Q[2, 2] = deltaT * sigmaxActual;

            Q[3, 3] = t5D20  * sigmayActual;
            Q[3, 4] = t4D8   * sigmayActual;
            Q[3, 5] = t3D6   * sigmayActual;
            Q[4, 3] = t4D8   * sigmayActual;
            Q[4, 4] = t3D3   * sigmayActual;
            Q[4, 5] = t2D2   * sigmayActual;
            Q[5, 3] = t3D6   * sigmayActual;
            Q[5, 4] = t2D2   * sigmayActual;
            Q[5, 5] = deltaT * sigmayActual;

            Q[6, 6] = t5D20  * sigmazActual;
            Q[6, 7] = t4D8   * sigmazActual;
            Q[6, 8] = t3D6   * sigmazActual;
            Q[7, 6] = t4D8   * sigmazActual;
            Q[7, 7] = t3D3   * sigmazActual;
            Q[7, 8] = t2D2   * sigmazActual;
            Q[8, 6] = t3D6   * sigmazActual;
            Q[8, 7] = t2D2   * sigmazActual;
            Q[8, 8] = deltaT * sigmazActual;

            return Q;
        }

        protected override Matrix getHMatrix()
        {
            if (H == null)
            {
                H = new Matrix(3, 9);
            }

            H[0, 0] = 1;
            H[1, 3] = 1;
            H[2, 6] = 1;

            return H;
        }

        protected override Matrix getRMatrix()
        {
            if (R == null)
            {
                R = new Matrix(3, 3);
            }

            R[0, 0] = sigmaxSensor * sigmaxSensor;  //This is the estimated variance (Std dev squared) for the X measurement
            R[1, 1] = sigmaySensor * sigmaySensor;  //This is the estimated variance (std dev squared) for the y measurement
            R[2, 2] = sigmazSensor * sigmazSensor;  //This is the estimated variance (std dev squared) for the z measurement
            //There should be no correlation between x, y, and z, thus they are left 0

            return R;
        }

        internal Matrix IntegrateMeasurement(Matrix measurement, DateTime time, Vector sensorSigmas)
        {
            sigmaxSensor = sensorSigmas[0];
            sigmaySensor = sensorSigmas[1];
            sigmazSensor = sensorSigmas[2];

            double deltaT = 0;
            if (lastTime.HasValue)
            {
                TimeSpan diff = time - lastTime.Value;
                deltaT = diff.TotalSeconds;
            }

            if (deltaT >= 0)
            {
                lastTime = time;
                return base.IntegrateMeasurement(measurement, deltaT);
            }
            else
            {
                return base.XLastMeasured;
            }
        }

        internal Matrix IntegrateMeasurement(Matrix measurement, DateTime time, double sensorSigmas)
        {
            sigmaxSensor = sensorSigmas;
            sigmaySensor = sensorSigmas;
            sigmazSensor = sensorSigmas;

            double deltaT = 0;
            if (lastTime.HasValue)
            {
                TimeSpan diff = time - lastTime.Value;
                deltaT = diff.TotalSeconds;
            }

            if (deltaT >= 0)
            {
                lastTime = time;
                return base.IntegrateMeasurement(measurement, deltaT);
            }
            else
            {
                return base.XLastMeasured;
            }
        }

        internal Matrix PredictAndDiscardFromNow(double deltaTFromNow)
        {
            if (lastTime.HasValue)
            {
                TimeSpan diff = DateTime.UtcNow - lastTime.Value;
                double deltaT = diff.TotalSeconds + deltaTFromNow;
                return base.PredictAndDiscard(deltaT);
            }
            else
            {
                return base.PredictAndDiscard(0);
            }
        }

        internal Matrix PredictAndDiscardFromNow(double deltaTFromNow, out Matrix covariance)
        {
            if (lastTime.HasValue)
            {
                TimeSpan diff = DateTime.UtcNow - lastTime.Value;
                double deltaT = diff.TotalSeconds + deltaTFromNow;
                return base.PredictAndDiscard(deltaT, out covariance);
            }
            else
            {
                return base.PredictAndDiscard(0, out covariance);
            }
        }
    }
}
