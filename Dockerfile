FROM quay.io/keycloak/keycloak:23.0.7

# Note: curl is not needed as Keycloak has built-in health endpoints

# Copy the email OTP provider (if it exists)
COPY providers/ /opt/keycloak/providers/

# Set environment variables
ENV KEYCLOAK_ADMIN=admin
ENV KEYCLOAK_ADMIN_PASSWORD=admin123
ENV KC_DB=postgres
ENV KC_DB_URL=jdbc:postgresql://postgres:5432/keycloak
ENV KC_DB_USERNAME=keycloak
ENV KC_DB_PASSWORD=keycloak123
ENV KC_HOSTNAME_STRICT=false
ENV KC_HOSTNAME_STRICT_HTTPS=false
ENV KC_HTTP_ENABLED=true

# Enable health and metrics
ENV KC_HEALTH_ENABLED=true
ENV KC_METRICS_ENABLED=true

# Email configuration for MailHog
ENV KC_SPI_EMAIL_DEFAULT_FROM=noreply@localhost
ENV KC_SPI_EMAIL_DEFAULT_REPLY_TO=noreply@localhost
ENV KC_SPI_EMAIL_DEFAULT_FROM_DISPLAY_NAME=Keycloak
ENV KC_SPI_EMAIL_SMTP_HOST=mailhog
ENV KC_SPI_EMAIL_SMTP_PORT=1025
ENV KC_SPI_EMAIL_SMTP_SSL=false
ENV KC_SPI_EMAIL_SMTP_STARTTLS=false

# Build the image
RUN /opt/keycloak/bin/kc.sh build

# Expose ports
EXPOSE 8080

# Start Keycloak
ENTRYPOINT ["/opt/keycloak/bin/kc.sh"]
CMD ["start-dev"]
